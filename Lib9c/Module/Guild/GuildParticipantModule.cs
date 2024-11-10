#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Bencodex.Types;
using Lib9c;
using Libplanet.Types.Assets;
using Nekoyume.Action.Guild.Migration;
using Nekoyume.Model.Guild;
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
            if (repository.TryGetGuildParticipant(target, out _))
            {
                throw new ArgumentException("The signer already joined a guild.");
            }

            if (repository.GetGuildRejoinCooldown(target) is { } cooldown
                && cooldown.Cooldown(repository.ActionContext.BlockIndex) > 0L)
            {
                throw new InvalidOperationException(
                    $"The signer is in the rejoin cooldown period until block {cooldown.ReleaseHeight}");
            }

            var guildParticipant = new GuildParticipant(target, guildAddress, repository);
            var guildGold = repository.GetBalance(guildParticipant.DelegationPoolAddress, Currencies.GuildGold);
            repository.SetGuildParticipant(guildParticipant);
            repository.IncreaseGuildMemberCount(guildAddress);
            if (guildGold.RawValue > 0)
            {
                repository.Delegate(target, guildGold);
            }

            return repository;
        }

        public static GuildRepository MoveGuild(
            this GuildRepository repository,
            AgentAddress guildParticipantAddress,
            GuildAddress dstGuildAddress)
        {
            var guildParticipant1 = repository.GetGuildParticipant(guildParticipantAddress);
            var srcGuild = repository.GetGuild(guildParticipant1.GuildAddress);
            var dstGuild = repository.GetGuild(dstGuildAddress);
            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            var srcValidatorDelegatee = validatorRepository.GetValidatorDelegatee(srcGuild.ValidatorAddress);
            var dstValidatorDelegatee = validatorRepository.GetValidatorDelegatee(dstGuild.ValidatorAddress);
            if (dstValidatorDelegatee.Tombstoned)
            {
                throw new InvalidOperationException("The validator of the guild to move to has been tombstoned.");
            }

            var guildParticipant2 = new GuildParticipant(guildParticipantAddress, dstGuildAddress, repository);
            var bond = validatorRepository.GetBond(srcValidatorDelegatee, guildParticipantAddress);
            repository.RemoveGuildParticipant(guildParticipantAddress);
            repository.DecreaseGuildMemberCount(guildParticipant1.GuildAddress);
            repository.SetGuildParticipant(guildParticipant2);
            repository.IncreaseGuildMemberCount(dstGuildAddress);
            if (bond.Share > 0)
            {
                repository.Redelegate(guildParticipantAddress, dstGuildAddress);
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

            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            var validatorDelegatee = validatorRepository.GetValidatorDelegatee(guild.ValidatorAddress);
            var bond = validatorRepository.GetBond(validatorDelegatee, agentAddress);

            repository.RemoveGuildParticipant(agentAddress);
            repository.DecreaseGuildMemberCount(guild.Address);
            if (bond.Share > 0)
            {
                repository.Undelegate(agentAddress);
            }

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

        // TODO: Hide this method as private after migration.
        public static GuildRepository Delegate(
            this GuildRepository repository,
            AgentAddress guildParticipantAddress,
            FungibleAssetValue fav)
        {
            var height = repository.ActionContext.BlockIndex;

            // TODO: Remove below unnecessary height condition after migration.
            height = Math.Max(height, GuildMigrationConfig.MigrateDelegationHeight);

            var guildParticipant = repository.GetGuildParticipant(guildParticipantAddress);
            var guild = repository.GetGuild(guildParticipant.GuildAddress);
            guildParticipant.Delegate(guild, fav, height);

            return repository;
        }

        private static GuildRepository Undelegate(
            this GuildRepository repository,
            AgentAddress guildParticipantAddress)
        {
            var height = repository.ActionContext.BlockIndex;
            var guildParticipant = repository.GetGuildParticipant(guildParticipantAddress);
            var guild = repository.GetGuild(guildParticipant.GuildAddress);
            var share = repository.GetBond(
                new ValidatorRepository(repository.World, repository.ActionContext)
                    .GetValidatorDelegatee(guild.ValidatorAddress),
                guildParticipantAddress).Share;
            guildParticipant.Undelegate(guild, share, height);

            return repository;
        }

        private static GuildRepository Undelegate(
            this GuildRepository repository,
            AgentAddress guildParticipantAddress,
            BigInteger share)
        {
            var height = repository.ActionContext.BlockIndex;
            var guildParticipant = repository.GetGuildParticipant(guildParticipantAddress);
            var guild = repository.GetGuild(guildParticipant.GuildAddress);
            guildParticipant.Undelegate(guild, share, height);

            return repository;
        }

        public static GuildRepository Redelegate(
            this GuildRepository repository,
            AgentAddress guildParticipantAddress,
            GuildAddress dstGuildAddress)
        {
            var height = repository.ActionContext.BlockIndex;
            var guildParticipant = repository.GetGuildParticipant(guildParticipantAddress);
            var guild = repository.GetGuild(guildParticipant.GuildAddress);
            var share = repository.GetBond(
                new ValidatorRepository(repository.World, repository.ActionContext)
                    .GetValidatorDelegatee(guild.ValidatorAddress),
                guildParticipantAddress).Share;
            var dstGuild = repository.GetGuild(dstGuildAddress);
            guildParticipant.Redelegate(guild, dstGuild, share, height);

            return repository;
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
