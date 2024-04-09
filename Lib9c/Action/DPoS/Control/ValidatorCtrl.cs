#nullable enable
using System;
using System.Collections.Immutable;
using System.Linq;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.DPoS.Exception;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Model;
using Nekoyume.Module;

namespace Nekoyume.Action.DPoS.Control
{
    internal static class ValidatorCtrl
    {
        internal static Validator? GetValidator(IWorldState states, Address validatorAddress)
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
                states = ValidatorSigningInfoCtrl.SetSigningInfo(
                    states,
                    new ValidatorSigningInfo { Address = validator.Address });
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
                throw new Exception.InvalidCurrencyException(Asset.GovernanceToken, governanceToken.Currency);
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

            Address delegationAddress = Delegation.DeriveAddress(
                operatorAddress, validator.Address);
            if (DelegateCtrl.GetDelegation(states, delegationAddress) is { } delegation)
            {
                states = ValidatorDelegationSetCtrl.Add(
                    states: states,
                    validatorAddress: validator.Address,
                    delegationAddress: delegationAddress
                );
            }

            // Does not save current instance, since it's done on delegation
            return states;
        }

        internal static FungibleAssetValue? ShareFromConsensusToken(
            IWorldState states, Address validatorAddress, FungibleAssetValue consensusToken)
        {
            if (!(GetValidator(states, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            FungibleAssetValue validatorConsensusToken
                = states.GetBalance(validator.Address, Asset.ConsensusToken);

            if (validator.DelegatorShares.Equals(Asset.Share * 0))
            {
                return FungibleAssetValue.FromRawValue(
                    Asset.Share, consensusToken.RawValue);
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
                var consensusToken = states.GetBalance(validator.Address, Asset.ConsensusToken);
                if (consensusToken.RawValue > 0)
                {
                    // Transfer consensus token to unbonded pool if remaining.
                    states = states.TransferAsset(
                        ctx,
                        ReservedAddress.BondedPool,
                        ReservedAddress.UnbondedPool,
                        Asset.GovernanceFromConsensus(
                            states.GetBalance(validator.Address, Asset.ConsensusToken)));
                }
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

        internal static FungibleAssetValue GetPower(IWorldState worldState, Address validatorAddress)
        {
            if (ValidatorPowerIndexCtrl.GetValidatorPowerIndex(worldState) is { } powerIndex)
            {
                return powerIndex.Index.First(item => item.ValidatorAddress == validatorAddress).ConsensusToken;
            }

            throw new ArgumentException("Validator power index not found.", nameof(validatorAddress));
        }

        internal static IWorld JailUntil(IWorld world, Address validatorAddress, long blockHeight)
            => ValidatorSigningInfoCtrl.JailUntil(world, validatorAddress, blockHeight);

        internal static IWorld Tombstone(IWorld world, Address validatorAddress)
            => ValidatorSigningInfoCtrl.Tombstone(world, validatorAddress);

        internal static bool IsTombstoned(IWorld world, Address validatorAddress)
            => ValidatorSigningInfoCtrl.IsTombstoned(world, validatorAddress);

        internal static IWorld Jail(IWorld world, Address validatorAddress)
        {
            if (!(GetValidator(world, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }
            if (validator.Jailed)
            {
                throw new JailedValidatorException(validator.Address);
            }

            validator.Jailed = true;
            world = world.SetDPoSState(validator.Address, validator.Serialize());
            world = ValidatorPowerIndexCtrl.Remove(world, validator.Address);
            return world;
        }

        internal static IWorld Unjail(IWorld world, Address validatorAddress)
        {
            if (!(GetValidator(world, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }
            if (!validator.Jailed)
            {
                throw new JailedValidatorException(validator.Address);
            }

            validator.Jailed = false;
            world = world.SetDPoSState(validator.Address, validator.Serialize());
            world = ValidatorPowerIndexCtrl.Update(world, validator.Address);
            return world;
        }

        internal static IWorld RemoveValidatorTokens(
            IWorld world,
            IActionContext actionContext,
            Address validatorAddress,
            FungibleAssetValue tokensToRemove)
        {
            if (GetValidator(world, validatorAddress) is not { } validator)
            {
                throw new NullValidatorException(validatorAddress);
            }

            world = world.BurnAsset(actionContext, validatorAddress, tokensToRemove);
            world = ValidatorPowerIndexCtrl.Update(world, validatorAddress);
            return world;
        }
    }
}
