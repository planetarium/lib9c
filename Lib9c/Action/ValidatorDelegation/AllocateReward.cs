using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Types.Assets;
using Libplanet.Types.Consensus;
using Libplanet.Types.Blocks;
using Nekoyume.Module;
using Nekoyume.Model.Guild;
using Nekoyume.Module.Guild;
using Nekoyume.ValidatorDelegation;
using Nekoyume.Module.ValidatorDelegation;

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
            var rewardCurrency = world.GetGoldCurrency();
            var repository = new GuildRepository(world, context);

            if (context.LastCommit is BlockCommit lastCommit)
            {
                var validatorSetPower = lastCommit.Votes.Aggregate(
                    BigInteger.Zero,
                    (total, next)
                        => total + (next.ValidatorPower ?? BigInteger.Zero));

                DistributeValidator(repository, rewardCurrency, validatorSetPower, lastCommit.Votes);
                var validatorRepository = new ValidatorRepository(repository.World, context);
                DistributeGuild(validatorRepository, rewardCurrency, validatorSetPower, lastCommit.Votes);
                repository.UpdateWorld(validatorRepository.World);
                DistributeGuildParticipant(repository, rewardCurrency, validatorSetPower, lastCommit.Votes);
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

        private static void DistributeValidator(
            GuildRepository repository,
            Currency rewardCurrency,
            BigInteger validatorSetPower,
            IEnumerable<Vote> lastVotes)
        {
            FungibleAssetValue reward
                = repository.GetBalance(Addresses.RewardPool, rewardCurrency);

            if (reward.Sign <= 0)
            {
                return;
            }

            if (validatorSetPower == BigInteger.Zero)
            {
                return;
            }

            var validatorReward = reward.DivRem(10).Quotient;
            var distributed = rewardCurrency * 0;
            foreach (Vote vote in lastVotes)
            {
                if (vote.Flag == VoteFlag.Null || vote.Flag == VoteFlag.Unknown)
                {
                    continue;
                }

                BigInteger validatorPower = vote.ValidatorPower ?? BigInteger.Zero;
                if (validatorPower == BigInteger.Zero)
                {
                    continue;
                }

                var validatorAddress = vote.ValidatorPublicKey.Address;
                if (!repository.TryGetGuildDelegatee(
                    validatorAddress, out var validatorDelegatee))
                {
                    continue;
                }


                FungibleAssetValue rewardEach
                    = (validatorReward * validatorPower).DivRem(validatorSetPower).Quotient;

                if (rewardEach.Sign > 0)
                {
                    repository.TransferAsset(Addresses.RewardPool, validatorAddress, rewardEach);
                    distributed += rewardEach;
                }
            }

            var remainder = validatorReward - distributed;
            if (remainder.Sign > 0)
            {
                repository.TransferAsset(Addresses.RewardPool, Addresses.CommunityPool, remainder);
            }
        }

        private static void DistributeGuild(
            ValidatorRepository repository,
            Currency rewardCurrency,
            BigInteger validatorSetPower,
            IEnumerable<Vote> lastVotes)
        {
            long blockHeight = repository.ActionContext.BlockIndex;

            FungibleAssetValue reward
                = repository.GetBalance(Addresses.RewardPool, rewardCurrency);

            if (reward.Sign <= 0)
            {
                return;
            }

            if (validatorSetPower == BigInteger.Zero)
            {
                return;
            }

            var guildReward = reward.DivRem(10).Quotient;
            var distributed = rewardCurrency * 0;

            foreach (Vote vote in lastVotes)
            {
                if (vote.Flag == VoteFlag.Null || vote.Flag == VoteFlag.Unknown)
                {
                    continue;
                }

                BigInteger validatorPower = vote.ValidatorPower ?? BigInteger.Zero;
                if (validatorPower == BigInteger.Zero)
                {
                    continue;
                }

                var validatorAddress = vote.ValidatorPublicKey.Address;
                if (!repository.TryGetValidatorDelegatee(
                    validatorAddress, out var validatorDelegatee))
                {
                    continue;
                }


                FungibleAssetValue rewardEach
                    = (guildReward * validatorPower).DivRem(validatorSetPower).Quotient;

                if (rewardEach.Sign > 0)
                {
                    repository.TransferAsset(
                        Addresses.RewardPool, validatorDelegatee.RewardPoolAddress, rewardEach);
                    validatorDelegatee.CollectRewards(blockHeight);
                    distributed += rewardEach;
                }
            }

            var remainder = guildReward - distributed;
            if (remainder.Sign > 0)
            {
                repository.TransferAsset(Addresses.RewardPool, Addresses.CommunityPool, remainder);
            }
        }

        private static void DistributeGuildParticipant(
            GuildRepository repository,
            Currency rewardCurrency,
            BigInteger validatorSetPower,
            IEnumerable<Vote> lastVotes)
        {
            long blockHeight = repository.ActionContext.BlockIndex;

            FungibleAssetValue reward
                = repository.GetBalance(Addresses.RewardPool, rewardCurrency);

            if (reward.Sign <= 0)
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

                BigInteger validatorPower = vote.ValidatorPower ?? BigInteger.Zero;
                if (validatorPower == BigInteger.Zero)
                {
                    continue;
                }

                var validatorAddress = vote.ValidatorPublicKey.Address;
                if (!repository.TryGetGuildDelegatee(
                    validatorAddress, out var validatorDelegatee))
                {
                    continue;
                }

                FungibleAssetValue rewardEach
                    = (reward * validatorPower).DivRem(validatorSetPower).Quotient;

                if (rewardEach.Sign > 0)
                {
                    repository.TransferAsset(Addresses.RewardPool, validatorDelegatee.RewardPoolAddress, rewardEach);
                    validatorDelegatee.CollectRewards(blockHeight);
                }
            }
        }
    }
}
