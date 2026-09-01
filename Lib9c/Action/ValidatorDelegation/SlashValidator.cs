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
using Serilog;

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
            Log.Information("[SlashValidator] Start block #{BlockIndex}", context.BlockIndex);

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
            Log.Information(
                "[SlashValidator] AbstainsToSlash count={Count}",
                abstainsToSlash.Count());

            foreach (var abstain in abstainsToSlash)
            {
                Log.Information(
                    "[SlashValidator] Slashing abstainer {Address}",
                    abstain.Address);
                var validatorDelegatee = repository.GetDelegatee(abstain.Address);
                if (validatorDelegatee.Jailed)
                {
                    Log.Information("[SlashValidator] Already jailed, skip {Address}", abstain.Address);
                    continue;
                }

                validatorDelegatee.Slash(LivenessSlashFactor, context.BlockIndex, context.BlockIndex);
                validatorDelegatee.Jail(context.BlockIndex + AbstainJailTime);

                var guildRepository = new GuildRepository(repository.World, repository.ActionContext);
                var guildDelegatee = guildRepository.GetDelegatee(abstain.Address);
                guildDelegatee.Slash(LivenessSlashFactor, context.BlockIndex, context.BlockIndex);
                guildDelegatee.Jail(context.BlockIndex + AbstainJailTime);
                repository.UpdateWorld(guildRepository.World);
                Log.Information("[SlashValidator] Slashed and jailed {Address}", abstain.Address);
            }

            Log.Information(
                "[SlashValidator] Processing evidence, count={Count}",
                context.Evidence.Count());
            foreach (var evidence in context.Evidence)
            {
                switch (evidence)
                {
                    case DuplicateVoteEvidence e:
                        Log.Information(
                            "[SlashValidator] DuplicateVoteEvidence target={Address}, height={Height}",
                            e.TargetAddress, e.Height);
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
                        Log.Information("[SlashValidator] DuplicateVote slashed {Address}", e.TargetAddress);
                        break;
                    default:
                        break;
                }
            }

            Log.Information("[SlashValidator] Complete block #{BlockIndex}", context.BlockIndex);
            return repository.World;
        }
    }
}
