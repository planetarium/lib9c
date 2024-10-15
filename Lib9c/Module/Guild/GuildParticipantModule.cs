#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Lib9c;
using Libplanet.Action.State;
using Libplanet.Types.Assets;
using Nekoyume.Model.Guild;
using Nekoyume.TypedAddress;

namespace Nekoyume.Module.Guild
{
    public static class GuildParticipantModule
    {
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
            AgentAddress guildParticipantAddress,
            GuildAddress guildAddress,
            long height)
            => repository
                .JoinGuild(guildAddress, new AgentAddress(guildParticipantAddress))
                .Delegate(
                    guildParticipantAddress,
                    guildAddress,
                    repository.World.GetBalance(guildParticipantAddress, Currencies.GuildGold),
                    height);

        public static GuildRepository LeaveGuildWithUndelegate(
            this GuildRepository repository,
            AgentAddress guildParticipantAddress,
            long height)
        {
            var guild = repository.GetJoinedGuild(guildParticipantAddress) is GuildAddress guildAddr
                ? repository.GetGuild(guildAddr)
                : throw new InvalidOperationException("The signer does not join any guild.");

            return repository
                .Undelegate(guildParticipantAddress,
                    guildAddr,
                    repository.GetBond(guild, guildParticipantAddress).Share,
                    height)
                .LeaveGuild(guildParticipantAddress);
        }

        public static GuildRepository MoveGuildWithRedelegate(
            this GuildRepository repository,
            AgentAddress guildParticipantAddress,
            GuildAddress srcGuildAddress,
            GuildAddress dstGuildAddress,
            long height)
        {
            var srcGuild = repository.GetGuild(srcGuildAddress);
            repository.Redelegate(
                guildParticipantAddress,
                srcGuildAddress,
                dstGuildAddress,
                repository.GetBond(srcGuild, guildParticipantAddress).Share,
                height);
            repository.LeaveGuild(guildParticipantAddress);
            repository.JoinGuild(dstGuildAddress, guildParticipantAddress);

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
            AgentAddress guildParticipantAddress,
            GuildAddress guildAddress,
            FungibleAssetValue fav,
            long height)
        {
            var guildParticipant = repository.GetGuildParticipant(guildParticipantAddress);
            var guild = repository.GetGuild(guildAddress);
            guildParticipant.Delegate(guild, fav, height);

            return repository;
        }

        private static GuildRepository Undelegate(
            this GuildRepository repository,
            AgentAddress guildParticipantAddress,
            GuildAddress guildAddress,
            BigInteger share,
            long height)
        {
            var guildParticipant = repository.GetGuildParticipant(guildParticipantAddress);
            var guild = repository.GetGuild(guildAddress);
            guildParticipant.Undelegate(guild, share, height);

            return repository;
        }

        public static GuildRepository Redelegate(
            this GuildRepository repository,
            AgentAddress guildParticipantAddress,
            GuildAddress srcGuildAddress,
            GuildAddress dstGuildAddress,
            BigInteger share,
            long height)
        {
            var guildParticipant = repository.GetGuildParticipant(guildParticipantAddress);
            var srcGuild = repository.GetGuild(srcGuildAddress);
            var dstGuild = repository.GetGuild(dstGuildAddress);
            guildParticipant.Redelegate(srcGuild, dstGuild, share, height);

            return repository;
        }

        private static GuildRepository ClaimReward(
            this GuildRepository repository,
            AgentAddress guildParticipantAddress,
            GuildAddress guildAddress,
            long height)
        {
            var guildParticipant = repository.GetGuildParticipant(guildParticipantAddress);
            var guild = repository.GetGuild(guildAddress);
            guildParticipant.ClaimReward(guild, height);

            return repository;
        }
    }
}
