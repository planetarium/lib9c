using System.Collections.Immutable;
using Lib9c.DPoS.Action;
using Lib9c.DPoS.Model;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.DPoS.Control
{
    internal static class ValidatorRewardsCtrl
    {
        internal static ValidatorRewards? GetValidatorRewards(
            IWorld states,
            Address validatorAddress)
        {
            if (states.GetDPoSState(validatorAddress) is { } value)
            {
                return new ValidatorRewards(value);
            }

            return null;
        }

        internal static (IWorld, ValidatorRewards) FetchValidatorRewards(
            IWorld states,
            Address validatorAddress,
            Currency currency)
        {
            Address validatorRewardsAddress
                = ValidatorRewards.DeriveAddress(validatorAddress, currency);
            ValidatorRewards validatorRewards;
            if (states.GetDPoSState(validatorRewardsAddress) is { } value)
            {
                validatorRewards = new ValidatorRewards(value);
            }
            else
            {
                validatorRewards = new ValidatorRewards(validatorAddress, currency);
                states = states.SetDPoSState(validatorRewards.Address, validatorRewards.Serialize());
            }

            return (states, validatorRewards);
        }

        internal static ImmutableSortedDictionary<long, FungibleAssetValue> RewardsBetween(
            IWorld states,
            Address validatorAddress,
            Currency currency,
            long minBlockHeight,
            long maxBlockHeight)
        {
            ValidatorRewards validatorRewards;
            (_, validatorRewards) = FetchValidatorRewards(states, validatorAddress, currency);
            return validatorRewards.Rewards.Where(
                kv => minBlockHeight <= kv.Key && kv.Key < maxBlockHeight)
                .ToImmutableSortedDictionary();
        }

        internal static FungibleAssetValue RewardSumBetween(
            IWorld states,
            Address validatorAddress,
            Currency currency,
            long minBlockHeight,
            long maxBlockHeight)
        {
            return RewardsBetween(
                states, validatorAddress, currency, minBlockHeight, maxBlockHeight)
                .Aggregate(currency * 0, (total, next) => total + next.Value);
        }

        internal static IWorld Add(
            IWorld states,
            Address validatorAddress,
            Currency currency,
            long blockHeight,
            FungibleAssetValue reward)
        {
            ValidatorRewards validatorRewards;
            (states, validatorRewards) = FetchValidatorRewards(states, validatorAddress, currency);
            validatorRewards.Add(blockHeight, reward);
            states = states.SetDPoSState(validatorRewards.Address, validatorRewards.Serialize());
            return states;
        }
    }
}
