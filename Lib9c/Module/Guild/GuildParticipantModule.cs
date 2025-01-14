#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Bencodex.Types;
using Lib9c;
using Libplanet.Types.Assets;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Model.Guild;
using Nekoyume.Module.ValidatorDelegation;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;

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

        public static GuildRepository JoinGuild(
            this GuildRepository repository,
            GuildAddress guildAddress,
            AgentAddress target)
        {
            var height = repository.ActionContext.BlockIndex;
            var signer = repository.ActionContext.Signer;
            if (repository.TryGetGuildParticipant(target, out _))
            {
                throw new InvalidOperationException("The signer already joined a guild.");
            }

            if (repository.GetGuildRejoinCooldown(target) is { } cooldown
                && cooldown.Cooldown(repository.ActionContext.BlockIndex) > 0L)
            {
                throw new InvalidOperationException(
                    $"The signer is in the rejoin cooldown period until block {cooldown.ReleaseHeight}");
            }

            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            if (validatorRepository.TryGetValidatorDelegatee(signer, out var _))
            {
                throw new InvalidOperationException("Validator cannot join a guild.");
            }

            var guildParticipant = new GuildParticipant(target, guildAddress, repository);
            var guildGold = repository.GetBalance(guildParticipant.DelegationPoolAddress, Currencies.GuildGold);
            var guild = repository.GetGuild(guildAddress);
            repository.SetGuildParticipant(guildParticipant);
            repository.IncreaseGuildMemberCount(guildAddress);
            if (guildGold.RawValue > 0)
            {
                guildParticipant.Delegate(guild, guildGold, height);
            }

            return repository;
        }

        public static GuildRepository MoveGuild(
            this GuildRepository repository,
            AgentAddress guildParticipantAddress,
            GuildAddress dstGuildAddress)
        {
            var height = repository.ActionContext.BlockIndex;
            var guildParticipant1 = repository.GetGuildParticipant(guildParticipantAddress);
            var srcGuild = repository.GetGuild(guildParticipant1.GuildAddress);
            var dstGuild = repository.GetGuild(dstGuildAddress);
            var srcDelegatee = repository.GetDelegatee(srcGuild.ValidatorAddress);
            var dstDelegatee = repository.GetDelegatee(dstGuild.ValidatorAddress);
            if (dstDelegatee.Tombstoned)
            {
                throw new InvalidOperationException("The validator of the guild to move to has been tombstoned.");
            }

            var guildParticipant2 = new GuildParticipant(guildParticipantAddress, dstGuildAddress, repository);
            var bond = repository.GetBond(srcDelegatee, srcGuild.Address);
            var share = bond.Share;
            repository.RemoveGuildParticipant(guildParticipantAddress);
            repository.DecreaseGuildMemberCount(guildParticipant1.GuildAddress);
            repository.SetGuildParticipant(guildParticipant2);
            repository.IncreaseGuildMemberCount(dstGuildAddress);
            if (share > 0)
            {
                guildParticipant1.Redelegate(srcGuild, dstGuild, share, height);
            }

            return repository;
        }

        public static GuildRepository LeaveGuild(
            this GuildRepository repository,
            AgentAddress agentAddress)
        {
            if (repository.GetJoinedGuild(agentAddress) is not { } guildAddress)
            {
                throw new InvalidOperationException("The signer does not join any guild.");
            }

            if (!repository.TryGetGuild(guildAddress, out var guild))
            {
                throw new InvalidOperationException(
                    "There is no such guild.");
            }

            if (guild.GuildMasterAddress == agentAddress)
            {
                throw new InvalidOperationException(
                    "The signer is a guild master. Guild master cannot quit the guild.");
            }

            var height = repository.ActionContext.BlockIndex;
            var guildParticipant = repository.GetGuildParticipant(agentAddress);
            var delegatee = repository.GetDelegatee(guildAddress);
            var bond = repository.GetBond(delegatee, agentAddress);
            var share = bond.Share;

            if (bond.Share > 0)
            {
                guildParticipant.Undelegate(guild, share, height);
            }

            repository.RemoveGuildParticipant(agentAddress);
            repository.DecreaseGuildMemberCount(guild.Address);

            repository.SetGuildRejoinCooldown(agentAddress, repository.ActionContext.BlockIndex);

            return repository;
        }

        public static bool TryGetGuildParticipant(
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

        private static GuildRepository SetGuildRejoinCooldown(
            this GuildRepository repository,
            AgentAddress guildParticipantAddress,
            long height)
        {
            var guildRejoinCooldown = new GuildRejoinCooldown(guildParticipantAddress, height);
            repository.UpdateWorld(
                repository.World.SetAccount(
                    Addresses.GuildRejoinCooldown,
                    repository.World.GetAccount(Addresses.GuildRejoinCooldown)
                        .SetState(guildParticipantAddress, guildRejoinCooldown.Bencoded)));
            return repository;
        }

        private static GuildRejoinCooldown? GetGuildRejoinCooldown(
            this GuildRepository repository,
            AgentAddress guildParticipantAddress)
            => repository.World
                .GetAccount(Addresses.GuildRejoinCooldown)
                .GetState(guildParticipantAddress) is Integer bencoded
                    ? new GuildRejoinCooldown(guildParticipantAddress, bencoded)
                    : null;
    }
}
