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
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Serilog;

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
            Log.Information(
                "[AllocateGuildReward] Start block #{BlockIndex}",
                context.BlockIndex);

            if (world.GetDelegationMigrationHeight() is not { } migrationHeight
                || context.BlockIndex < migrationHeight)
            {
                Log.Information("[AllocateGuildReward] Skipped (migration height not reached)");
                return world;
            }

            Log.Information("[AllocateGuildReward] Creating ValidatorRepository...");
            var repository = new ValidatorRepository(world, context);
            Log.Information("[AllocateGuildReward] ValidatorRepository created");
            var rewardCurrency = Currencies.Mead;
            var proposerInfo = repository.GetProposerInfo();
            Log.Information("[AllocateGuildReward] ProposerInfo retrieved");

            if (context.LastCommit is BlockCommit lastCommit)
            {
                var validatorSetPower = lastCommit.Votes.Aggregate(
                    BigInteger.Zero,
                    (total, next)
                        => total + (next.ValidatorPower ?? BigInteger.Zero));

                Log.Information(
                    "[AllocateGuildReward] DistributeProposerReward start, votes={VoteCount}, power={Power}",
                    lastCommit.Votes.Count(),
                    validatorSetPower);
                DistributeProposerReward(
                    repository,
                    rewardCurrency,
                    proposerInfo,
                    validatorSetPower,
                    lastCommit.Votes);
                Log.Information("[AllocateGuildReward] DistributeProposerReward done");

                Log.Information("[AllocateGuildReward] DistributeValidatorReward start");
                DistributeValidatorReward(
                    repository,
                    rewardCurrency,
                    validatorSetPower,
                    lastCommit.Votes);
                Log.Information("[AllocateGuildReward] DistributeValidatorReward done");
            }

            var communityFund = repository.GetBalance(Addresses.RewardPool, rewardCurrency);
            Log.Information(
                "[AllocateGuildReward] CommunityFund={CommunityFund}",
                communityFund);

            if (communityFund.Sign > 0)
            {
                repository.TransferAsset(
                    Addresses.RewardPool,
                    Addresses.CommunityPool,
                    communityFund);
            }

            Log.Information("[AllocateGuildReward] Complete block #{BlockIndex}", context.BlockIndex);
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
                Log.Information("[AllocateGuildReward.DistributeValidatorReward] No reward to allocate");
                return;
            }

            if (validatorSetPower == BigInteger.Zero)
            {
                Log.Information("[AllocateGuildReward.DistributeValidatorReward] ValidatorSetPower is zero");
                return;
            }

            int index = 0;
            foreach (Vote vote in lastVotes)
            {
                if (vote.Flag == VoteFlag.Null || vote.Flag == VoteFlag.Unknown)
                {
                    continue;
                }

                var validatorAddress = vote.ValidatorPublicKey.Address;
                Log.Information(
                    "[AllocateGuildReward.DistributeValidatorReward] Processing validator {Index} {Address}",
                    index, validatorAddress);

                if (!repository.TryGetDelegatee(
                    validatorAddress, out var validatorDelegatee))
                {
                    Log.Information(
                        "[AllocateGuildReward.DistributeValidatorReward] Delegatee not found for {Address}",
                        validatorAddress);
                    continue;
                }

                BigInteger validatorPower = vote.ValidatorPower ?? BigInteger.Zero;
                if (validatorPower == BigInteger.Zero)
                {
                    continue;
                }

                Log.Information(
                    "[AllocateGuildReward.DistributeValidatorReward] AllocateReward for {Address}, power={Power}",
                    validatorAddress, validatorPower);
                validatorDelegatee.AllocateReward(
                    rewardToAllocate,
                    validatorPower,
                    validatorSetPower,
                    Addresses.RewardPool,
                    blockHeight);
                Log.Information(
                    "[AllocateGuildReward.DistributeValidatorReward] AllocateReward done for {Address}",
                    validatorAddress);
                index++;
            }
        }
    }
}
