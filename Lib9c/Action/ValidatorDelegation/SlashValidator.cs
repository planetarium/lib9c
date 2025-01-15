using System;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Types.Consensus;
using Libplanet.Types.Evidence;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Model.Guild;
using Nekoyume.Module.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Action.ValidatorDelegation
{
    public sealed class SlashValidator : ActionBase
    {
        public SlashValidator()
        {
        }

        public static BigInteger DuplicateVoteSlashFactor => 10;

        public static BigInteger LivenessSlashFactor => 10;

        public static long AbstainJailTime => 10L;

        public override IValue PlainValue => Null.Value;

        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public override IWorld Execute(IActionContext context)
        {
            var world = context.PreviousState;

            if (world.GetDelegationMigrationHeight() is null)
            {
                return world;
            }

            var repository = new ValidatorRepository(world, context);

            var abstainHistory = repository.GetAbstainHistory();
            var abstainsToSlash = abstainHistory.FindToSlashAndAdd(
                context.LastCommit.Votes.Where(vote => vote.Flag == VoteFlag.Null)
                    .Select(vote => vote.ValidatorPublicKey),
                context.BlockIndex);
            repository.SetAbstainHistory(abstainHistory);

            foreach (var abstain in abstainsToSlash)
            {
                var validatorDelegatee = repository.GetDelegatee(abstain.Address);
                if (validatorDelegatee.Jailed)
                {
                    continue;
                }

                validatorDelegatee.Slash(LivenessSlashFactor, context.BlockIndex, context.BlockIndex);
                validatorDelegatee.Jail(context.BlockIndex + AbstainJailTime);

                var guildRepository = new GuildRepository(repository.World, repository.ActionContext);
                var guildDelegatee = guildRepository.GetDelegatee(abstain.Address);
                guildDelegatee.Slash(LivenessSlashFactor, context.BlockIndex, context.BlockIndex);
                guildDelegatee.Jail(context.BlockIndex + AbstainJailTime);
                repository.UpdateWorld(guildRepository.World);
            }

            foreach (var evidence in context.Evidence)
            {
                switch (evidence)
                {
                    case DuplicateVoteEvidence e:
                        if (e.Height > context.BlockIndex)
                        {
                            throw new Exception("Evidence height is greater than block index.");
                        }

                        var validatorDelegatee = repository.GetDelegatee(e.TargetAddress);
                        validatorDelegatee.Slash(DuplicateVoteSlashFactor, e.Height, context.BlockIndex);
                        validatorDelegatee.Tombstone();

                        var guildRepository = new GuildRepository(repository.World, repository.ActionContext);
                        var guildDelegatee = guildRepository.GetDelegatee(e.TargetAddress);
                        guildDelegatee.Slash(DuplicateVoteSlashFactor, e.Height, context.BlockIndex);
                        guildDelegatee.Tombstone();
                        repository.UpdateWorld(guildRepository.World);
                        break;
                    default:
                        break;
                }
            }

            return repository.World;
        }
    }
}
