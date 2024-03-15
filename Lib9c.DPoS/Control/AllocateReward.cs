using System.Collections.Immutable;
using System.Numerics;
using Lib9c.DPoS.Misc;
using Lib9c.DPoS.Model;
using Lib9c.DPoS.Util;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.PoS;
using Libplanet.Types.Assets;
using Libplanet.Types.Consensus;
using Nekoyume.Module;
using Validator = Lib9c.DPoS.Model.Validator;
using ValidatorSet = Lib9c.DPoS.Model.ValidatorSet;

namespace Lib9c.DPoS.Control
{
    public static class AllocateReward
    {
        public static BigInteger BaseProposerRewardNumer => 1;

        public static BigInteger BaseProposerRewardDenom => 100;

        public static BigInteger BonusProposerRewardNumer => 4;

        public static BigInteger BonusProposerRewardDenom => 100;

        public static Address RewardAddress(Address holderAddress)
        {
            return holderAddress.Derive("RewardAddress");
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

            FungibleAssetValue votePowerNumer
                = votes.Aggregate(
                    Asset.ConsensusToken * 0, (total, next)
                    => total + bondedValidatorDict[next.ValidatorPublicKey].ConsensusToken);

            FungibleAssetValue votePowerDenom
                = bondedValidatorSet.TotalConsensusToken;

            var (baseProposerReward, _)
                = (blockReward * BaseProposerRewardNumer).DivRem(BaseProposerRewardDenom);
            var (bonusProposerReward, _)
                = (blockReward * votePowerNumer.RawValue * BonusProposerRewardNumer)
                .DivRem(votePowerDenom.RawValue * BonusProposerRewardDenom);
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

                FungibleAssetValue powerNumer
                    = bondedValidator.ConsensusToken;

                FungibleAssetValue powerDenom
                    = bondedValidatorSet.TotalConsensusToken;

                var (validatorReward, _)
                    = (validatorRewardSum * powerNumer.RawValue)
                    .DivRem(powerDenom.RawValue);
                var (commission, _)
                    = (validatorReward * Validator.CommissionNumer)
                    .DivRem(Validator.CommissionDenom);

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
