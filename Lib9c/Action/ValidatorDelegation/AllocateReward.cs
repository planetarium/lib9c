using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Types.Assets;
using Libplanet.Types.Consensus;
using Nekoyume.ValidatorDelegation;
using Nekoyume.Module.ValidatorDelegation;
using Nekoyume.Module;

namespace Nekoyume.Action.ValidatorDelegation
{
    public class AllocateReward : ActionBase
    {
        public const string TypeIdentifier = "distribute_validators";

        private const string TargetKey = "t";

        public AllocateReward()
        {
        }

        public override IValue PlainValue => Dictionary.Empty;

        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public override IWorld Execute(IActionContext context)
        {
            var world = context.PreviousState;
            var rewardCurrency = world.GetGoldCurrency();

            var proposerInfo = world.GetProposerInfo();

            world = DistributeProposerReward(
                world,
                context,
                rewardCurrency,
                proposerInfo,
                context.LastCommit.Votes);

            world = DistributeValidatorReward(
                world,
                context,
                rewardCurrency,
                context.LastCommit.Votes);

            var communityFund = world.GetBalance(Addresses.RewardPool, rewardCurrency);

            if (communityFund.Sign > 0)
            {
                world = world.TransferAsset(
                    context,
                    Addresses.RewardPool,
                    Addresses.CommunityPool,
                    communityFund);
            }

            return world;
        }

        internal static IWorld DistributeProposerReward(
            IWorld states,
            IActionContext ctx,
            Currency rewardCurrency,
            ProposerInfo proposerInfo,
            IEnumerable<Vote> lastVotes)
        {
            FungibleAssetValue blockReward = states.GetBalance(
                Addresses.RewardPool, rewardCurrency);

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
                        => total +
                            (next.Flag == VoteFlag.PreCommit
                                ? next.ValidatorPower ?? BigInteger.Zero
                                : BigInteger.Zero));

            BigInteger votePowerDenominator
                = lastVotes.Aggregate(
                    BigInteger.Zero,
                    (total, next)
                        => total + (next.ValidatorPower ?? BigInteger.Zero));

            if (votePowerDenominator == BigInteger.Zero)
            {
                return states;
            }

            var baseProposerReward
                = (blockReward * ValidatorDelegatee.BaseProposerRewardNumerator)
                .DivRem(ValidatorDelegatee.BaseProposerRewardDenominator).Quotient;
            var bonusProposerReward
                = (blockReward * votePowerNumerator * ValidatorDelegatee.BonusProposerRewardNumerator)
                .DivRem(votePowerDenominator * ValidatorDelegatee.BonusProposerRewardDenominator).Quotient;
            FungibleAssetValue proposerReward = baseProposerReward + bonusProposerReward;

            states = states.TransferAsset(
                ctx,
                Addresses.RewardPool,
                proposerInfo.Proposer,
                proposerReward);

            return states;
        }

        internal static IWorld DistributeValidatorReward(
            IWorld states,
            IActionContext ctx,
            Currency rewardCurrency,
            IEnumerable<Vote> lastVotes)
        {
            long blockHeight = ctx.BlockIndex;
            var repo = new ValidatorRepository(states, ctx);

            FungibleAssetValue rewardToAllocate
                = states.GetBalance(Addresses.RewardPool, rewardCurrency);

            if (rewardToAllocate.Sign <= 0)
            {
                return states;
            }

            BigInteger validatorSetPower
                = lastVotes.Aggregate(
                    BigInteger.Zero,
                    (total, next) => total + (next.ValidatorPower ?? BigInteger.Zero));

            if (validatorSetPower == BigInteger.Zero)
            {
                return states;
            }

            foreach (Vote vote in lastVotes)
            {
                if (vote.Flag == VoteFlag.Null || vote.Flag == VoteFlag.Unknown)
                {
                    continue;
                }

                if (!states.TryGetValidatorDelegatee(
                    vote.ValidatorPublicKey.Address, repo, out var validatorDelegatee))
                {
                    continue;
                }

                BigInteger validatorPower = vote.ValidatorPower ?? BigInteger.Zero;

                if (validatorPower == BigInteger.Zero)
                {
                    continue;
                }

                validatorDelegatee.AllocateReward(
                    rewardToAllocate,
                    validatorPower,
                    validatorSetPower,
                    Addresses.RewardPool,
                    blockHeight);

                states = validatorDelegatee.Repository.World;
            }

            return states;
        }
    }
}
