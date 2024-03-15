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
    internal static class Bond
    {
        internal static (IWorld, FungibleAssetValue) Execute(
            IWorld states,
            IActionContext ctx,
            FungibleAssetValue consensusToken,
            Address validatorAddress,
            Address delegationAddress,
            IImmutableSet<Currency> nativeTokens)
        {
            // TODO: Failure condition
            // 1. Validator does not exist
            // 2. Exchange rate is invalid(validator has no tokens but there are outstanding shares)
            // 3. Amount is less than the minimum amount
            // 4. Delegator does not have sufficient consensus token (fail or apply maximum)
            if (!consensusToken.Currency.Equals(Asset.ConsensusToken))
            {
                throw new InvalidCurrencyException(Asset.ConsensusToken, consensusToken.Currency);
            }

            if (!(ValidatorCtrl.GetValidator(states, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            // If validator share is zero, exchange rate is 1
            // Else, exchange rate is validator share / token
            if (!(ValidatorCtrl.ShareFromConsensusToken(
                states, validator.Address, consensusToken) is { } issuedShare))
            {
                throw new InvalidExchangeRateException(validator.Address);
            }

            // Mint consensus token to validator
            states = states.MintAsset(ctx, validator.Address, consensusToken);

            // Mint share to delegation
            states = states.MintAsset(ctx, delegationAddress, issuedShare);

            // Track total shares minted from validator
            validator.DelegatorShares += issuedShare;
            states = states.SetDPoSState(validator.Address, validator.Serialize());
            states = ValidatorPowerIndexCtrl.Update(states, validator.Address);

            ValidatorDelegationSet validatorDelegationSet;
            (states, validatorDelegationSet) =
                ValidatorDelegationSetCtrl.FetchValidatorDelegationSet(states, validator.Address);

            foreach (Address addrs in validatorDelegationSet.Set)
            {
                states = DelegateCtrl.Distribute(states, ctx, nativeTokens, addrs);
            }

            return (states, issuedShare);
        }

        internal static (IWorld, FungibleAssetValue) Cancel(
            IWorld states,
            IActionContext ctx,
            FungibleAssetValue share,
            Address validatorAddress,
            Address delegationAddress,
            IImmutableSet<Currency> nativeTokens)
        {
            long blockHeight = ctx.BlockIndex;

            // Currency check
            if (!share.Currency.Equals(Asset.Share))
            {
                throw new InvalidCurrencyException(Asset.Share, share.Currency);
            }

            FungibleAssetValue delegationShareBalance = states.GetBalance(
                delegationAddress, Asset.Share);
            if (share > delegationShareBalance)
            {
                throw new InsufficientFungibleAssetValueException(
                    share,
                    delegationShareBalance,
                    $"Delegation {delegationAddress} has insufficient share");
            }

            if (!(ValidatorCtrl.GetValidator(states, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            // Delegator share burn
            states = states.BurnAsset(ctx, delegationAddress, share);

            // Jailing check
            FungibleAssetValue delegationShare = states.GetBalance(delegationAddress, Asset.Share);
            if (delegationAddress.Equals(validator.OperatorAddress)
                && !validator.Jailed
                && ValidatorCtrl.ConsensusTokenFromShare(states, validator.Address, delegationShare)
                < Validator.MinSelfDelegation)
            {
                validator.Jailed = true;
            }

            // Calculate consensus token amount
            if (!(ValidatorCtrl.ConsensusTokenFromShare(
                states, validator.Address, share) is { } unbondingConsensusToken))
            {
                throw new InvalidExchangeRateException(validator.Address);
            }

            if (share.Equals(validator.DelegatorShares))
            {
                unbondingConsensusToken = states.GetBalance(
                    validator.Address, Asset.ConsensusToken);
            }

            // Subtracting from DelegatorShare have to be calculated last
            // since it will affect ConsensusTokenFromShare()
            validator.DelegatorShares -= share;
            states = states.BurnAsset(ctx, validator.Address, unbondingConsensusToken);
            states = states.SetDPoSState(validator.Address, validator.Serialize());

            states = ValidatorPowerIndexCtrl.Update(states, validator.Address);

            ValidatorDelegationSet validatorDelegationSet;
            (states, validatorDelegationSet) =
                ValidatorDelegationSetCtrl.FetchValidatorDelegationSet(states, validator.Address);

            foreach (Address addrs in validatorDelegationSet.Set)
            {
                states = DelegateCtrl.Distribute(states, ctx, nativeTokens, addrs);
            }

            return (states, unbondingConsensusToken);
        }
    }
}
