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
using Libplanet.Types.Blocks;

namespace Nekoyume.Action.ValidatorDelegation
{
    public sealed class AllocateReward : ActionBase
    {
        public AllocateReward()
        {
        }

        public override IValue PlainValue => Null.Value;

        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public override IWorld Execute(IActionContext context)
        {
            var world = context.PreviousState;
            var repository = new ValidatorRepository(world, context);
            var rewardCurrency = ValidatorDelegatee.ValidatorRewardCurrency;
            var proposerInfo = repository.GetProposerInfo();

            if (context.LastCommit is BlockCommit lastCommit)
            {
                DistributeProposerReward(
                    repository,
                    context,
                    rewardCurrency,
                    proposerInfo,
                    lastCommit.Votes);

                DistributeValidatorReward(
                    repository,
                    context,
                    rewardCurrency,
                    lastCommit.Votes);
            }

            var communityFund = repository.GetBalance(Addresses.RewardPool, rewardCurrency);

            if (communityFund.Sign > 0)
            {
                repository.TransferAsset(
                    Addresses.RewardPool,
                    Addresses.CommunityPool,
                    communityFund);
            }

            return repository.World;
        }

        internal static void DistributeProposerReward(
            ValidatorRepository repository,
            IActionContext ctx,
            Currency rewardCurrency,
            ProposerInfo proposerInfo,
            IEnumerable<Vote> lastVotes)
        {
            FungibleAssetValue blockReward = repository.GetBalance(
                Addresses.RewardPool, rewardCurrency);

            if (proposerInfo.BlockIndex != ctx.BlockIndex - 1)
            {
                return;
            }

            if (blockReward.Sign <= 0)
            {
                return;
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
                return;
            }

            var baseProposerReward
                = (blockReward * ValidatorDelegatee.BaseProposerRewardPercentage)
                .DivRem(100).Quotient;
            var bonusProposerReward
                = (blockReward * votePowerNumerator * ValidatorDelegatee.BonusProposerRewardPercentage)
                .DivRem(votePowerDenominator * 100).Quotient;
            FungibleAssetValue proposerReward = baseProposerReward + bonusProposerReward;

            if (proposerReward.Sign > 0)
            {
                repository.TransferAsset(
                    Addresses.RewardPool,
                    proposerInfo.Proposer,
                    proposerReward);
            }
        }

        internal static void DistributeValidatorReward(
            ValidatorRepository repository,
            IActionContext ctx,
            Currency rewardCurrency,
            IEnumerable<Vote> lastVotes)
        {
            long blockHeight = ctx.BlockIndex;

            FungibleAssetValue rewardToAllocate
                = repository.GetBalance(Addresses.RewardPool, rewardCurrency);

            if (rewardToAllocate.Sign <= 0)
            {
                return;
            }

            BigInteger validatorSetPower
                = lastVotes.Aggregate(
                    BigInteger.Zero,
                    (total, next) => total + (next.ValidatorPower ?? BigInteger.Zero));

            if (validatorSetPower == BigInteger.Zero)
            {
                return;
            }

            foreach (Vote vote in lastVotes)
            {
                if (vote.Flag == VoteFlag.Null || vote.Flag == VoteFlag.Unknown)
                {
                    continue;
                }

                if (!repository.TryGetValidatorDelegatee(
                    vote.ValidatorPublicKey.Address, out var validatorDelegatee))
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
            }
        }
    }
}
