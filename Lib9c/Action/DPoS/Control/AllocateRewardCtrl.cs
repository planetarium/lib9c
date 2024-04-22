#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
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
            ProposerInfo proposerInfo)
        {
            if (nativeTokens is null)
            {
                throw new NullNativeTokensException();
            }

            foreach (Currency nativeToken in nativeTokens)
            {
                if (votes is { } lastVotesEnumerable)
                {
                    var lastVotes = lastVotesEnumerable.ToArray();
                    states = DistributeProposerReward(
                        states, ctx, nativeToken, proposerInfo, lastVotes);

                    // TODO: Check if this is correct?
                    states = DistributeValidatorReward(
                        states, ctx, nativeToken, lastVotes);
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
            ProposerInfo proposerInfo,
            Vote[] lastVotes)
        {
            FungibleAssetValue blockReward = states.GetBalance(
                ReservedAddress.RewardPool, nativeToken);

            if (proposerInfo.BlockIndex != ctx.BlockIndex - 1)
            {
                return states;
            }

            if (blockReward.Sign <= 0)
            {
                return states;
            }

            BigInteger votePowerNumerator
                = lastVotes.Aggregate(
                    BigInteger.Zero,
                    (total, next)
                        => total + (next.Flag == VoteFlag.PreCommit ? next.ValidatorPower : 0));

            BigInteger votePowerDenominator
                = lastVotes.Aggregate(
                    BigInteger.Zero,
                    (total, next)
                        => total + next.ValidatorPower);

            var (baseProposerReward, _)
                = (blockReward * BaseProposerRewardNumerator).DivRem(BaseProposerRewardDenominator);
            var (bonusProposerReward, _)
                = (blockReward * votePowerNumerator * BonusProposerRewardNumerator)
                .DivRem(votePowerDenominator * BonusProposerRewardDenominator);
            FungibleAssetValue proposerReward = baseProposerReward + bonusProposerReward;

            states = states.TransferAsset(
                ctx,
                ReservedAddress.RewardPool,
                RewardAddress(proposerInfo.Proposer),
                proposerReward);

            return states;
        }

        internal static IWorld DistributeValidatorReward(
            IWorld states,
            IActionContext ctx,
            Currency nativeToken,
            Vote[] lastVotes)
        {
            long blockHeight = ctx.BlockIndex;
            FungibleAssetValue validatorRewardSum = states.GetBalance(
                ReservedAddress.RewardPool, nativeToken);

            if (validatorRewardSum.Sign <= 0)
            {
                return states;
            }

            BigInteger powerDenominator
                = lastVotes.Aggregate(
                    BigInteger.Zero,
                    (total, next)
                        => total + next.ValidatorPower);

            foreach (Vote vote in lastVotes)
            {
                if (vote.Flag == VoteFlag.Null || vote.Flag == VoteFlag.Unknown)
                {
                    continue;
                }

                BigInteger powerNumerator = vote.ValidatorPower;

                var (validatorReward, _)
                    = (validatorRewardSum * powerNumerator)
                    .DivRem(powerDenominator);
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
                    ValidatorRewards.DeriveAddress(vote.ValidatorPublicKey.Address, nativeToken),
                    delegationRewardSum);

                states = ValidatorRewardsCtrl.Add(
                    states,
                    vote.ValidatorPublicKey.Address,
                    nativeToken,
                    blockHeight,
                    delegationRewardSum);
            }

            return states;
        }
    }
}
