using System.Collections.Immutable;
using Lib9c.DPoS.Action;
using Lib9c.DPoS.Exception;
using Lib9c.DPoS.Misc;
using Lib9c.DPoS.Model;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Module;

namespace Lib9c.DPoS.Control
{
    internal static class ValidatorCtrl
    {
        internal static Validator? GetValidator(
            IWorld states,
            Address validatorAddress)
        {
            if (states.GetDPoSState(validatorAddress) is { } value)
            {
                return new Validator(value);
            }

            return null;
        }

        internal static (IWorld, Validator) FetchValidator(
            IWorld states,
            Address operatorAddress,
            PublicKey operatorPublicKey)
        {
            if (!operatorAddress.Equals(operatorPublicKey.Address))
            {
                throw new PublicKeyAddressMatchingException(operatorAddress, operatorPublicKey);
            }

            Address validatorAddress = Validator.DeriveAddress(operatorAddress);
            Validator validator;
            if (states.GetDPoSState(validatorAddress) is { } value)
            {
                validator = new Validator(value);
            }
            else
            {
                validator = new Validator(operatorAddress, operatorPublicKey);
                states = states.SetDPoSState(validator.Address, validator.Serialize());
            }

            return (states, validator);
        }

        internal static IWorld Create(
            IWorld states,
            IActionContext ctx,
            Address operatorAddress,
            PublicKey operatorPublicKey,
            FungibleAssetValue governanceToken,
            IImmutableSet<Currency> nativeTokens)
        {
            if (!governanceToken.Currency.Equals(Asset.GovernanceToken))
            {
                throw new InvalidCurrencyException(Asset.GovernanceToken, governanceToken.Currency);
            }

            FungibleAssetValue consensusToken = Asset.ConsensusFromGovernance(governanceToken);
            if (consensusToken < Validator.MinSelfDelegation)
            {
                throw new InsufficientFungibleAssetValueException(
                    Validator.MinSelfDelegation, consensusToken, "Insufficient self delegation");
            }

            Address validatorAddress = Validator.DeriveAddress(operatorAddress);
            if (states.GetDPoSState(validatorAddress) != null)
            {
                throw new DuplicatedValidatorException(validatorAddress);
            }

            Validator validator;
            (states, validator) = FetchValidator(states, operatorAddress, operatorPublicKey);

            states = DelegateCtrl.Execute(
                states,
                ctx,
                operatorAddress,
                validator.Address,
                governanceToken,
                nativeTokens);

            // Does not save current instance, since it's done on delegation
            return states;
        }

        internal static FungibleAssetValue? ShareFromConsensusToken(
            IWorld states, Address validatorAddress, FungibleAssetValue consensusToken)
        {
            if (!(GetValidator(states, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            FungibleAssetValue validatorConsensusToken
                = states.GetBalance(validator.Address, Asset.ConsensusToken);

            if (validator.DelegatorShares.Equals(Asset.Share * 0))
            {
                return new FungibleAssetValue(
                    Asset.Share, consensusToken.MajorUnit, consensusToken.MinorUnit);
            }

            if (validatorConsensusToken.RawValue == 0)
            {
                return null;
            }

            FungibleAssetValue share
                = (validator.DelegatorShares
                * consensusToken.RawValue)
                .DivRem(validatorConsensusToken.RawValue, out _);

            return share;
        }

        internal static FungibleAssetValue? TokenPortionByShare(
            IWorld states,
            Address validatorAddress,
            FungibleAssetValue token,
            FungibleAssetValue share)
        {
            if (!(GetValidator(states, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            if (validator.DelegatorShares.RawValue == 0)
            {
                return null;
            }

            var (tokenPortion, _)
                = (token * share.RawValue)
                .DivRem(validator.DelegatorShares.RawValue);

            return tokenPortion;
        }

        internal static FungibleAssetValue? ConsensusTokenFromShare(
            IWorld states, Address validatorAddress, FungibleAssetValue share)
        {
            if (!(GetValidator(states, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            FungibleAssetValue validatorConsensusToken
                = states.GetBalance(validator.Address, Asset.ConsensusToken);

            // Is below conditional statement right?
            // Need to be investigated
            if (validatorConsensusToken.RawValue == 0)
            {
                return null;
            }

            if (validator.DelegatorShares.RawValue == 0)
            {
                return null;
            }

            FungibleAssetValue consensusToken
                = (validatorConsensusToken
                * share.RawValue)
                .DivRem(validator.DelegatorShares.RawValue, out _);

            return consensusToken;
        }

        internal static IWorld Bond(
            IWorld states,
            IActionContext ctx,
            Address validatorAddress)
        {
            if (!(GetValidator(states, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            validator.UnbondingCompletionBlockHeight = -1;
            if (validator.Status != BondingStatus.Bonded)
            {
                states = states.TransferAsset(
                    ctx,
                    ReservedAddress.UnbondedPool,
                    ReservedAddress.BondedPool,
                    Asset.GovernanceFromConsensus(
                        states.GetBalance(validator.Address, Asset.ConsensusToken)));
            }

            validator.Status = BondingStatus.Bonded;
            states = states.SetDPoSState(validator.Address, validator.Serialize());
            return states;
        }

        internal static IWorld Unbond(
            IWorld states,
            IActionContext ctx,
            Address validatorAddress)
        {
            long blockHeight = ctx.BlockIndex;
            if (!(GetValidator(states, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            validator.UnbondingCompletionBlockHeight = blockHeight + UnbondingSet.Period;
            if (validator.Status == BondingStatus.Bonded)
            {
                states = states.TransferAsset(
                    ctx,
                    ReservedAddress.BondedPool,
                    ReservedAddress.UnbondedPool,
                    Asset.GovernanceFromConsensus(
                        states.GetBalance(validator.Address, Asset.ConsensusToken)));
            }

            validator.Status = BondingStatus.Unbonding;
            states = states.SetDPoSState(validator.Address, validator.Serialize());

            states = UnbondingSetCtrl.AddValidatorAddressSet(states, validator.Address);

            return states;
        }

        internal static IWorld Complete(
            IWorld states,
            IActionContext ctx,
            Address validatorAddress)
        {
            long blockHeight = ctx.BlockIndex;
            if (!(GetValidator(states, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            if (!validator.IsMatured(blockHeight) || (validator.Status != BondingStatus.Unbonding))
            {
                return states;
            }

            validator.Status = BondingStatus.Unbonded;
            states = states.SetDPoSState(validator.Address, validator.Serialize());

            states = UnbondingSetCtrl.RemoveValidatorAddressSet(states, validator.Address);

            // Later implemented get rid of validator
            if (validator.DelegatorShares == Asset.Share * 0)
            {
            }

            return states;
        }
    }
}
