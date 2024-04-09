#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Consensus;
using Nekoyume.Action.DPoS.Exception;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Model;
using Nekoyume.Action.DPoS.Util;
using Nekoyume.Module;
using Validator = Nekoyume.Action.DPoS.Model.Validator;
using ValidatorSet = Nekoyume.Action.DPoS.Model.ValidatorSet;

namespace Nekoyume.Action.DPoS.Control
{
    public static class AllocateRewardCtrl
    {
        public static BigInteger BaseProposerRewardNumerator => 1;

        public static BigInteger BaseProposerRewardDenominator => 100;

        public static BigInteger BonusProposerRewardNumerator => 4;

        public static BigInteger BonusProposerRewardDenominator => 100;

        public static Address RewardAddress(Address holderAddress)
        {
            return AddressHelper.Derive(holderAddress, "RewardAddress");
        }

        internal static IWorld Execute(
            IWorld states,
            IActionContext ctx,
            IImmutableSet<Currency>? nativeTokens,
            IEnumerable<Vote>? votes,
            Address miner)
        {
            ValidatorSet bondedValidatorSet;
            (states, bondedValidatorSet) = ValidatorSetCtrl.FetchBondedValidatorSet(states);

            if (nativeTokens is null)
            {
                throw new NullNativeTokensException();
            }

            foreach (Currency nativeToken in nativeTokens)
            {
                if (votes is { } lastVotes)
                {
                    states = DistributeProposerReward(
                        states, ctx, nativeToken, miner, bondedValidatorSet, lastVotes);

                    // TODO: Check if this is correct?
                    states = DistributeValidatorReward(
                        states, ctx, nativeToken, bondedValidatorSet, votes);
                }

                FungibleAssetValue communityFund = states.GetBalance(
                    ReservedAddress.RewardPool, nativeToken);

                if (communityFund.Sign > 0)
                {
                    states = states.TransferAsset(
                        ctx,
                        ReservedAddress.RewardPool,
                        ReservedAddress.CommunityPool,
                        communityFund);
                }
            }

            return states;
        }

        internal static IWorld DistributeProposerReward(
            IWorld states,
            IActionContext ctx,
            Currency nativeToken,
            Address proposer,
            ValidatorSet bondedValidatorSet,
            IEnumerable<Vote> votes)
        {
            FungibleAssetValue blockReward = states.GetBalance(
                ReservedAddress.RewardPool, nativeToken);

            if (blockReward.Sign <= 0)
            {
                return states;
            }

            ImmutableDictionary<PublicKey, ValidatorPower> bondedValidatorDict
                = bondedValidatorSet.Set.ToImmutableDictionary(
                    bondedValidator => bondedValidator.OperatorPublicKey);

            FungibleAssetValue votePowerNumerator
                = votes.Aggregate(
                    Asset.ConsensusToken * 0, (total, next)
                    => total + bondedValidatorDict[next.ValidatorPublicKey].ConsensusToken);

            FungibleAssetValue votePowerDenominator
                = bondedValidatorSet.TotalConsensusToken;

            var (baseProposerReward, _)
                = (blockReward * BaseProposerRewardNumerator).DivRem(BaseProposerRewardDenominator);
            var (bonusProposerReward, _)
                = (blockReward * votePowerNumerator.RawValue * BonusProposerRewardNumerator)
                .DivRem(votePowerDenominator.RawValue * BonusProposerRewardDenominator);
            FungibleAssetValue proposerReward = baseProposerReward + bonusProposerReward;

            states = states.TransferAsset(
                ctx, ReservedAddress.RewardPool, RewardAddress(proposer), proposerReward);

            return states;
        }

        internal static IWorld DistributeValidatorReward(
            IWorld states,
            IActionContext ctx,
            Currency nativeToken,
            ValidatorSet bondedValidatorSet,
            IEnumerable<Vote> votes)
        {
            long blockHeight = ctx.BlockIndex;
            FungibleAssetValue validatorRewardSum = states.GetBalance(
                ReservedAddress.RewardPool, nativeToken);

            if (validatorRewardSum.Sign <= 0)
            {
                return states;
            }

            ImmutableDictionary<PublicKey, ValidatorPower> bondedValidatorDict
                = bondedValidatorSet.Set.ToImmutableDictionary(
                    bondedValidator => bondedValidator.OperatorPublicKey);

            foreach (Vote vote in votes)
            {
                if (vote.Flag == VoteFlag.Null || vote.Flag == VoteFlag.Unknown)
                {
                    continue;
                }

                ValidatorPower bondedValidator = bondedValidatorDict[vote.ValidatorPublicKey];

                FungibleAssetValue powerNumerator
                    = bondedValidator.ConsensusToken;

                FungibleAssetValue powerDenominator
                    = bondedValidatorSet.TotalConsensusToken;

                var (validatorReward, _)
                    = (validatorRewardSum * powerNumerator.RawValue)
                    .DivRem(powerDenominator.RawValue);
                var (commission, _)
                    = (validatorReward * Validator.CommissionNumerator)
                    .DivRem(Validator.CommissionDenominator);

                FungibleAssetValue delegationRewardSum = validatorReward - commission;

                states = states.TransferAsset(
                    ctx,
                    ReservedAddress.RewardPool,
                    RewardAddress(vote.ValidatorPublicKey.Address),
                    commission);

                states = states.TransferAsset(
                    ctx,
                    ReservedAddress.RewardPool,
                    ValidatorRewards.DeriveAddress(bondedValidator.ValidatorAddress, nativeToken),
                    delegationRewardSum);

                states = ValidatorRewardsCtrl.Add(
                    states,
                    bondedValidator.ValidatorAddress,
                    nativeToken,
                    blockHeight,
                    delegationRewardSum);
            }

            return states;
        }
    }
}
