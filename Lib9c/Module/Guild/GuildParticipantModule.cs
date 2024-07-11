#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Bencodex.Types;
using Libplanet.Action.State;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.TypedAddress;

namespace Nekoyume.Module.Guild
{
    public static class GuildParticipantModule
    {
        // Returns `null` when it didn't join any guild.
        // Returns `GuildAddress` when it joined a guild.
        public static GuildAddress? GetJoinedGuild(this IWorldState worldState, AgentAddress agentAddress)
        {
            return worldState.TryGetGuildParticipant(agentAddress, out var guildParticipant)
                ? guildParticipant.GuildAddress
                : null;
        }

        public static IWorld JoinGuild(
            this IWorld world,
            GuildAddress guildAddress,
            AgentAddress agentAddress)
        {
            var guildParticipant = new Model.Guild.GuildParticipant(guildAddress);
            return world.MutateAccount(Addresses.GuildParticipant,
                    account => account.SetState(agentAddress, guildParticipant.Bencoded))
                .IncreaseGuildMemberCount(guildAddress);
        }

        public static IWorld LeaveGuild(
            this IWorld world,
            AgentAddress agentAddress)
        {
            if (!world.TryGetGuildParticipant(agentAddress, out var guildParticipant))
            {
                throw new InvalidOperationException("It may not join any guild.");
            }

            return world.RemoveGuildParticipant(agentAddress)
                .DecreaseGuildMemberCount(guildParticipant.GuildAddress);
        }

        private static Model.Guild.GuildParticipant GetGuildParticipant(this IWorldState worldState, AgentAddress agentAddress)
        {
            var value = worldState.GetAccountState(Addresses.GuildParticipant)
                .GetState(agentAddress);
            if (value is List list)
            {
                return new Model.Guild.GuildParticipant(list);
            }

            throw new FailedLoadStateException("It may not join any guild.");
        }

        private static bool TryGetGuildParticipant(this IWorldState worldState,
            AgentAddress agentAddress,
            [NotNullWhen(true)] out Model.Guild.GuildParticipant? guildParticipant)
        {
            try
            {
                guildParticipant = GetGuildParticipant(worldState, agentAddress);
                return true;
            }
            catch
            {
                guildParticipant = null;
                return false;
            }
        }

        private static IWorld RemoveGuildParticipant(this IWorld world, AgentAddress agentAddress)
        {
            return world.MutateAccount(Addresses.GuildParticipant,
                account => account.RemoveState(agentAddress));
        }
    }
}
