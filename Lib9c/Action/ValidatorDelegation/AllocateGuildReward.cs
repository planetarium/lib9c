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
using Lib9c;

namespace Nekoyume.Action.ValidatorDelegation
{
    public sealed class AllocateGuildReward : ActionBase
    {
        public AllocateGuildReward()
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
            var rewardCurrency = Currencies.Mead;
            var proposerInfo = repository.GetProposerInfo();

            if (context.LastCommit is BlockCommit lastCommit)
            {
                var validatorSetPower = lastCommit.Votes.Aggregate(
                    BigInteger.Zero,
                    (total, next)
                        => total + (next.ValidatorPower ?? BigInteger.Zero));

                DistributeProposerReward(
                    repository,
                    rewardCurrency,
                    proposerInfo,
                    validatorSetPower,
                    lastCommit.Votes);

                DistributeValidatorReward(
                    repository,
                    rewardCurrency,
                    validatorSetPower,
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

        private static void DistributeProposerReward(
            ValidatorRepository repository,
            Currency rewardCurrency,
            ProposerInfo proposerInfo,
            BigInteger validatorSetPower,
            IEnumerable<Vote> lastVotes)
        {
            FungibleAssetValue blockReward = repository.GetBalance(
                Addresses.RewardPool, rewardCurrency);

            if (proposerInfo.BlockIndex != repository.ActionContext.BlockIndex - 1)
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

            BigInteger votePowerDenominator = validatorSetPower;

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

        private static void DistributeValidatorReward(
            ValidatorRepository repository,
            Currency rewardCurrency,
            BigInteger validatorSetPower,
            IEnumerable<Vote> lastVotes)
        {
            long blockHeight = repository.ActionContext.BlockIndex;

            FungibleAssetValue rewardToAllocate
                = repository.GetBalance(Addresses.RewardPool, rewardCurrency);

            if (rewardToAllocate.Sign <= 0)
            {
                return;
            }

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
