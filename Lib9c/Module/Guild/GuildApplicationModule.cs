#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Bencodex.Types;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Model.Guild;
using Nekoyume.TypedAddress;

namespace Nekoyume.Module.Guild
{
    public static class GuildApplicationModule
    {
        public static Model.Guild.GuildApplication GetGuildApplication(
            this GuildRepository repository, AgentAddress agentAddress)
        {
            var value = repository.World.GetAccountState(Addresses.GuildApplication).GetState(agentAddress);
            if (value is List list)
            {
                return new Model.Guild.GuildApplication(list);
            }

            throw new FailedLoadStateException("There is no such guild.");
        }

        public static bool TryGetGuildApplication(this GuildRepository repository,
            AgentAddress agentAddress, [NotNullWhen(true)] out Model.Guild.GuildApplication? guildApplication)
        {
            try
            {
                guildApplication = repository.GetGuildApplication(agentAddress);
                return true;
            }
            catch
            {
                guildApplication = null;
                return false;
            }
        }

        public static void ApplyGuild(
            this GuildRepository repository, AgentAddress signer, GuildAddress guildAddress)
        {
            if (repository.GetJoinedGuild(signer) is not null)
            {
                throw new InvalidOperationException("The signer is already joined in a guild.");
            }

            // NOTE: Check there is such guild.
            if (!repository.TryGetGuild(guildAddress, out _))
            {
                throw new InvalidOperationException("The guild does not exist.");
            }

            if (repository.IsBanned(guildAddress, signer))
            {
                throw new InvalidOperationException("The signer is banned from the guild.");
            }

            repository.SetGuildApplication(signer, new GuildApplication(guildAddress));
        }

        public static void CancelGuildApplication(
            this GuildRepository repository, AgentAddress agentAddress)
        {
            if (!repository.TryGetGuildApplication(agentAddress, out _))
            {
                throw new InvalidOperationException("It may not apply any guild.");
            }

            repository.RemoveGuildApplication(agentAddress);
        }

        public static void AcceptGuildApplication(
            this GuildRepository repository, AgentAddress signer, AgentAddress target, long height)
        {
            if (!repository.TryGetGuildApplication(target, out var guildApplication))
            {
                throw new InvalidOperationException("It may not apply any guild.");
            }

            if (!repository.TryGetGuild(guildApplication.GuildAddress, out var guild))
            {
                throw new InvalidOperationException(
                    "There is no such guild now. It may be removed. Please cancel and apply another guild.");
            }

            if (signer != guild.GuildMasterAddress)
            {
                throw new InvalidOperationException("It may not be a guild master.");
            }

            repository.RemoveGuildApplication(target);
            repository.JoinGuild(guildApplication.GuildAddress, target);
        }

#pragma warning disable S4144
        public static void RejectGuildApplication(
#pragma warning restore S4144
            this GuildRepository repository, AgentAddress signer, AgentAddress target)
        {
            if (!repository.TryGetGuildApplication(target, out var guildApplication))
            {
                throw new InvalidOperationException("It may not apply any guild.");
            }

            if (!repository.TryGetGuild(guildApplication.GuildAddress, out var guild))
            {
                throw new InvalidOperationException(
                    "There is no such guild now. It may be removed. Please cancel and apply another guild.");
            }

            if (signer != guild.GuildMasterAddress)
            {
                throw new InvalidOperationException("It may not be a guild master.");
            }

            repository.RemoveGuildApplication(target);
        }

        public static void SetGuildApplication(
            this GuildRepository repository, AgentAddress agentAddress, GuildApplication guildApplication)
        {
            repository.UpdateWorld(
                repository.World.MutateAccount(
                    Addresses.GuildApplication,
                    account => account.SetState(agentAddress, guildApplication.Bencoded)));
        }

        private static void RemoveGuildApplication(
            this GuildRepository repository, AgentAddress agentAddress)
        {
            repository.UpdateWorld(
                repository.World.MutateAccount(
                    Addresses.GuildApplication,
                    account => account.RemoveState(agentAddress)));
        }
    }
}
