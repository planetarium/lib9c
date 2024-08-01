#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Model.Guild;
using Nekoyume.TypedAddress;

namespace Nekoyume.Module.Guild
{
    public static class GuildApplicationModule
    {
        public static Model.Guild.GuildApplication GetGuildApplication(this IWorldState worldState, AgentAddress agentAddress)
        {
            var value = worldState.GetAccountState(Addresses.GuildApplication).GetState(agentAddress);
            if (value is List list)
            {
                return new Model.Guild.GuildApplication(list);
            }

            throw new FailedLoadStateException("There is no such guild.");
        }

        public static bool TryGetGuildApplication(this IWorldState worldState,
            AgentAddress agentAddress, [NotNullWhen(true)] out Model.Guild.GuildApplication? guildApplication)
        {
            try
            {
                guildApplication = GetGuildApplication(worldState, agentAddress);
                return true;
            }
            catch
            {
                guildApplication = null;
                return false;
            }
        }

        public static IWorld ApplyGuild(
            this IWorld world, AgentAddress signer, GuildAddress guildAddress)
        {
            if (world.GetJoinedGuild(signer) is not null)
            {
                throw new InvalidOperationException("The signer is already joined in a guild.");
            }

            // NOTE: Check there is such guild.
            if (!world.TryGetGuild(guildAddress, out _))
            {
                throw new InvalidOperationException("The guild does not exist.");
            }

            if (world.IsBanned(guildAddress, signer))
            {
                throw new InvalidOperationException("The signer is banned from the guild.");
            }

            return world.MutateAccount(Addresses.GuildApplication,
                account =>
                    account.SetState(signer, new GuildApplication(guildAddress).Bencoded));
        }

        public static IWorld CancelGuildApplication(
            this IWorld world, AgentAddress agentAddress)
        {
            if (!world.TryGetGuildApplication(agentAddress, out _))
            {
                throw new InvalidOperationException("It may not apply any guild.");
            }

            return world.RemoveGuildApplication(agentAddress);
        }

        public static IWorld AcceptGuildApplication(
            this IWorld world, AgentAddress signer, AgentAddress target)
        {
            if (!world.TryGetGuildApplication(target, out var guildApplication))
            {
                throw new InvalidOperationException("It may not apply any guild.");
            }

            if (!world.TryGetGuild(guildApplication.GuildAddress, out var guild))
            {
                throw new InvalidOperationException(
                    "There is no such guild now. It may be removed. Please cancel and apply another guild.");
            }

            if (signer != guild.GuildMasterAddress)
            {
                throw new InvalidOperationException("It may not be a guild master.");
            }

            return world.RemoveGuildApplication(target)
                .JoinGuild(guildApplication.GuildAddress, target);
        }

#pragma warning disable S4144
        public static IWorld RejectGuildApplication(
#pragma warning restore S4144
            this IWorld world, AgentAddress signer, AgentAddress target)
        {
            if (!world.TryGetGuildApplication(target, out var guildApplication))
            {
                throw new InvalidOperationException("It may not apply any guild.");
            }

            if (!world.TryGetGuild(guildApplication.GuildAddress, out var guild))
            {
                throw new InvalidOperationException(
                    "There is no such guild now. It may be removed. Please cancel and apply another guild.");
            }

            if (signer != guild.GuildMasterAddress)
            {
                throw new InvalidOperationException("It may not be a guild master.");
            }

            return world.RemoveGuildApplication(target);
        }

        private static IWorld RemoveGuildApplication(this IWorld world, AgentAddress agentAddress)
        {
            return world.MutateAccount(Addresses.GuildApplication,
                account => account.RemoveState(agentAddress));
        }

    }
}
