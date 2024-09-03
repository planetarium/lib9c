#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Types.Assets;
using Nekoyume.Model.Guild;
using Nekoyume.TypedAddress;

namespace Nekoyume.Module.Guild
{
    public static class GuildParticipantModule
    {
        public static GuildRepository GetGuildRepository(this IWorld world, IActionContext context)
            => new GuildRepository(world, context);

        // Returns `null` when it didn't join any guild.
        // Returns `GuildAddress` when it joined a guild.
        public static GuildAddress? GetJoinedGuild(this GuildRepository repository, AgentAddress agentAddress)
        {
            return repository.TryGetGuildParticipant(agentAddress, out var guildParticipant)
                ? guildParticipant.GuildAddress
                : null;
        }

        public static GuildRepository JoinGuildWithDelegate(
            this GuildRepository repository,
            IActionContext context,
            GuildAddress guildAddress)
            => repository
                .JoinGuild(guildAddress, new AgentAddress(context.Signer))
                .Delegate(context, guildAddress, repository.World.GetBalance(context.Signer, Currencies.GuildGold));

        public static GuildRepository LeaveGuildWithUndelegate(
            this GuildRepository repository,
            IActionContext context)
        {
            var guild = repository.GetJoinedGuild(new AgentAddress(context.Signer)) is GuildAddress guildAddr
                ? repository.GetGuild(guildAddr)
                : throw new InvalidOperationException("The signer does not join any guild.");

            return repository
                .Undelegate(context, guildAddr, repository.GetBond(guild, context.Signer).Share)
                .LeaveGuild(new AgentAddress(context.Signer));
        }

        public static GuildRepository MoveGuildWithRedelegate(
            this GuildRepository repository,
            IActionContext context,
            GuildAddress srcGuildAddress,
            GuildAddress dstGuildAddress)
        {
            var agentAddress = new AgentAddress(context.Signer);
            var srcGuild = repository.GetGuild(srcGuildAddress);
            repository.Redelegate(context, srcGuildAddress, dstGuildAddress, repository.GetBond(srcGuild, agentAddress).Share);
            repository.LeaveGuild(agentAddress);
            repository.JoinGuild(dstGuildAddress, agentAddress);

            return repository;
        }

        public static GuildRepository JoinGuild(
            this GuildRepository repository,
            GuildAddress guildAddress,
            AgentAddress target)
        {
            var guildParticipant = new Model.Guild.GuildParticipant(target, guildAddress, repository);
            repository.SetGuildParticipant(guildParticipant);
            repository.IncreaseGuildMemberCount(guildAddress);

            return repository;
        }

        public static GuildRepository LeaveGuild(
            this GuildRepository repository,
            AgentAddress target)
        {
            if (repository.GetJoinedGuild(target) is not { } guildAddress)
            {
                throw new InvalidOperationException("The signer does not join any guild.");
            }

            if (!repository.TryGetGuild(guildAddress, out var guild))
            {
                throw new InvalidOperationException(
                    "There is no such guild.");
            }

            if (guild.GuildMasterAddress == target)
            {
                throw new InvalidOperationException(
                    "The signer is a guild master. Guild master cannot quit the guild.");
            }

            return repository.RawLeaveGuild(target);
        }

        public static GuildRepository RawLeaveGuild(this GuildRepository repository, AgentAddress target)
        {
            if (!repository.TryGetGuildParticipant(target, out var guildParticipant))
            {
                throw new InvalidOperationException("It may not join any guild.");
            }

            repository.RemoveGuildParticipant(target);
            repository.DecreaseGuildMemberCount(guildParticipant.GuildAddress);

            return repository;
        }

        private static bool TryGetGuildParticipant(
            this GuildRepository repository,
            AgentAddress agentAddress,
            [NotNullWhen(true)] out Model.Guild.GuildParticipant? guildParticipant)
        {
            try
            {
                guildParticipant = repository.GetGuildParticipant(agentAddress);
                return true;
            }
            catch
            {
                guildParticipant = null;
                return false;
            }
        }

        private static GuildRepository Delegate(
            this GuildRepository repository,
            IActionContext context,
            GuildAddress guildAddress,
            FungibleAssetValue fav)
        {
            var agentAddress = new AgentAddress(context.Signer);
            var guildParticipant = repository.GetGuildParticipant(agentAddress);
            var guild = repository.GetGuild(guildAddress);
            guildParticipant.Delegate(guild, fav, context.BlockIndex);

            return repository;
        }

        private static GuildRepository Undelegate(
            this GuildRepository repository,
            IActionContext context,
            GuildAddress guildAddress,
            BigInteger share)
        {
            var agentAddress = new AgentAddress(context.Signer);
            var guildParticipant = repository.GetGuildParticipant(agentAddress);
            var guild = repository.GetGuild(guildAddress);
            guildParticipant.Undelegate(guild, share, context.BlockIndex);

            return repository;
        }

        public static GuildRepository Redelegate(
            this GuildRepository repository,
            IActionContext context,
            GuildAddress srcGuildAddress,
            GuildAddress dstGuildAddress,
            BigInteger share)
        {
            var agentAddress = new AgentAddress(context.Signer);
            var guildParticipant = repository.GetGuildParticipant(agentAddress);
            var srcGuild = repository.GetGuild(srcGuildAddress);
            var dstGuild = repository.GetGuild(dstGuildAddress);
            guildParticipant.Redelegate(srcGuild, dstGuild, share, context.BlockIndex);

            return repository;
        }

        private static GuildRepository ClaimReward(
            this GuildRepository repository,
            IActionContext context,
            GuildAddress guildAddress)
        {
            var agentAddress = new AgentAddress(context.Signer);
            var guildParticipant = repository.GetGuildParticipant(agentAddress);
            var guild = repository.GetGuild(guildAddress);
            guildParticipant.ClaimReward(guild, context.BlockIndex);

            return repository;
        }
    }
}
