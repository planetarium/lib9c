#nullable enable
using System.Collections.Immutable;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.DPoS.Exception;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Model;
using Nekoyume.Action.DPoS.Util;
using Nekoyume.Module;
using Serilog;

namespace Nekoyume.Action.DPoS.Control
{
    internal static class DelegateCtrl
    {
        internal static Delegation? GetDelegation(IWorldState states, Address delegationAddress)
        {
            if (states.GetDPoSState(delegationAddress) is { } value)
            {
                return new Delegation(value);
            }

            return null;
        }

        internal static Delegation? GetDelegation(IWorldState states, Address delegatorAddress, Address validatorAddress)
        {
            Address delegationAddress = Delegation.DeriveAddress(
                delegatorAddress, validatorAddress);
            return GetDelegation(states, delegationAddress);
        }

        internal static (IWorld, Delegation) FetchDelegation(
            IWorld states,
            Address delegatorAddress,
            Address validatorAddress)
        {
            Address delegationAddress = Delegation.DeriveAddress(
                delegatorAddress, validatorAddress);
            Delegation delegation;
            if (states.GetDPoSState(delegationAddress) is { } value)
            {
                delegation = new Delegation(value);
            }
            else
            {
                delegation = new Delegation(delegatorAddress, validatorAddress);
                states = states.SetDPoSState(delegation.Address, delegation.Serialize());
            }

            return (states, delegation);
        }

        internal static IWorld Execute(
           IWorld states,
           IActionContext ctx,
           Address delegatorAddress,
           Address validatorAddress,
           FungibleAssetValue governanceToken,
           IImmutableSet<Currency> nativeTokens)
        {
            if (!governanceToken.Currency.Equals(Asset.GovernanceToken))
            {
                throw new Exception.InvalidCurrencyException(Asset.GovernanceToken, governanceToken.Currency);
            }

            FungibleAssetValue delegatorGovernanceTokenBalance = states.GetBalance(
                delegatorAddress, Asset.GovernanceToken);
            if (governanceToken > delegatorGovernanceTokenBalance)
            {
                throw new InsufficientFungibleAssetValueException(
                    governanceToken,
                    delegatorGovernanceTokenBalance,
                    $"Delegator {delegatorAddress} has insufficient governanceToken");
            }

            if (!(ValidatorCtrl.GetValidator(states, validatorAddress) is { } validator))
            {
                throw new NullValidatorException(validatorAddress);
            }

            Delegation? delegation;
            (states, delegation) = FetchDelegation(states, delegatorAddress, validatorAddress);

            FungibleAssetValue consensusToken = Asset.ConsensusFromGovernance(governanceToken);
            Address poolAddress = validator.Status == BondingStatus.Bonded
                ? ReservedAddress.BondedPool
                : ReservedAddress.UnbondedPool;

            states = states.TransferAsset(
                ctx, delegatorAddress, poolAddress, governanceToken);
            (states, _) = Bond.Execute(
                states,
                ctx,
                consensusToken,
                delegation.ValidatorAddress,
                delegation.Address,
                nativeTokens);

            states = states.SetDPoSState(delegation.Address, delegation.Serialize());

            return states;
        }

        internal static IWorld Distribute(
           IWorld states,
           IActionContext ctx,
           IImmutableSet<Currency> nativeTokens,
           Address delegationAddress)
        {
            long blockHeight = ctx.BlockIndex;
            if (!(GetDelegation(states, delegationAddress) is { } delegation))
            {
                throw new NullDelegationException(delegationAddress);
            }

            if (!(ValidatorCtrl.GetValidator(states, delegation.ValidatorAddress) is { } validator))
            {
                throw new NullValidatorException(delegation.ValidatorAddress);
            }

            foreach (Currency nativeToken in nativeTokens)
            {
                FungibleAssetValue delegationRewardSum = ValidatorRewardsCtrl.RewardSumBetween(
                    states,
                    delegation.ValidatorAddress,
                    nativeToken,
                    delegation.LatestDistributeHeight,
                    blockHeight);

                // Skip if there is no reward to distribute.
                if (delegationRewardSum.RawValue == 0)
                {
                    continue;
                }

                if (!(ValidatorCtrl.TokenPortionByShare(
                    states,
                    delegation.ValidatorAddress,
                    delegationRewardSum,
                    states.GetBalance(delegationAddress, Asset.Share)) is { } reward))
                {
                    throw new InvalidExchangeRateException(validator.Address);
                }

                if (reward.Sign > 0)
                {
                    Address validatorRewardAddress
                        = ValidatorRewards.DeriveAddress(delegation.ValidatorAddress, nativeToken);

                    states = states.TransferAsset(
                        ctx,
                        validatorRewardAddress,
                        AllocateRewardCtrl.RewardAddress(delegation.DelegatorAddress),
                        reward);
                }
            }

            delegation.LatestDistributeHeight = blockHeight;

            states = states.SetDPoSState(delegation.Address, delegation.Serialize());

            return states;
        }
    }
}
