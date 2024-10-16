#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Lib9c;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
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
            var guildParticipant = new GuildParticipant(target, guildAddress, repository);
            var guildGold = repository.GetBalance(guildAddress, Currencies.GuildGold);
            repository.SetGuildParticipant(guildParticipant);
            repository.IncreaseGuildMemberCount(guildAddress);
            repository.Delegate(target, guildGold);

            return repository;
        }

        public static GuildRepository MoveGuild(
            this GuildRepository repository,
            AgentAddress guildParticipantAddress,
            GuildAddress dstGuildAddress)
        {
            var dstGuild = repository.GetGuild(dstGuildAddress);
            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            var dstValidatorDelegatee = validatorRepository.GetValidatorDelegatee(dstGuild.ValidatorAddress);
            if (dstValidatorDelegatee.Tombstoned)
            {
                throw new InvalidOperationException("The validator of the guild to move to has been tombstoned.");
            }

            var guildParticipant = repository.GetGuildParticipant(guildParticipantAddress);
            repository.RemoveGuildParticipant(guildParticipantAddress);
            repository.DecreaseGuildMemberCount(guildParticipant.GuildAddress);
            repository.SetGuildParticipant(guildParticipant);
            repository.IncreaseGuildMemberCount(dstGuildAddress);
            repository.Redelegate(
                guildParticipantAddress, dstGuildAddress);

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

            repository.RemoveGuildParticipant(target);
            repository.DecreaseGuildMemberCount(guild.Address);
            repository.Undelegate(target);

            return repository;
        }

        // public static GuildRepository RawLeaveGuild(this GuildRepository repository, AgentAddress target)
        // {
        //     if (!repository.TryGetGuildParticipant(target, out var guildParticipant))
        //     {
        //         throw new InvalidOperationException("It may not join any guild.");
        //     }

        //     repository.RemoveGuildParticipant(target);
        //     repository.DecreaseGuildMemberCount(guildParticipant.GuildAddress);

        //     return repository;
        // }

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
            FungibleAssetValue fav)
        {
            var height = repository.ActionContext.BlockIndex;
            var guildParticipant = repository.GetGuildParticipant(guildParticipantAddress);
            var guild = repository.GetGuild(guildParticipant.GuildAddress);
            var validatorAddress = guild.ValidatorAddress;
            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            var validatorDelegatee = validatorRepository.GetValidatorDelegatee(validatorAddress);
            var validatorDelegator = validatorRepository.GetValidatorDelegator(
                guildParticipantAddress, guild.Address);
            validatorDelegator.Delegate(validatorDelegatee, fav, height);
            repository.UpdateWorld(validatorRepository.World);
            guild.Bond(guildParticipant, fav, height);

            return repository;
        }

        private static GuildRepository Undelegate(
            this GuildRepository repository,
            AgentAddress guildParticipantAddress)
        {
            var height = repository.ActionContext.BlockIndex;
            var guildParticipant = repository.GetGuildParticipant(guildParticipantAddress);
            var guild = repository.GetGuild(guildParticipant.GuildAddress);
            var validatorAddress = guild.ValidatorAddress;
            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            var validatorDelegatee = validatorRepository.GetValidatorDelegatee(validatorAddress);
            var validatorDelegator = validatorRepository.GetValidatorDelegator(
                guildParticipantAddress, guild.Address);
            var bond = validatorRepository.GetBond(validatorDelegatee, guildParticipantAddress);
            var share = bond.Share;
            validatorDelegator.Undelegate(validatorDelegatee, share, height);
            repository.UpdateWorld(validatorRepository.World);
            var guildShare = repository.GetBond(guild, guildParticipantAddress).Share;
            guild.Unbond(guildParticipant, guildShare, height);

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
            var validatorAddress = guild.ValidatorAddress;
            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            var validatorDelegatee = validatorRepository.GetValidatorDelegatee(validatorAddress);
            var validatorDelegator = validatorRepository.GetValidatorDelegator(
                guildParticipantAddress, guild.Address);
            validatorDelegator.Undelegate(validatorDelegatee, share, height);
            repository.UpdateWorld(validatorRepository.World);
            var guildShare = repository.GetBond(guild, guildParticipantAddress).Share;
            guild.Unbond(guildParticipant, guildShare, height);

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
            var srcValidatorAddress = guild.ValidatorAddress;
            var dstGuild = repository.GetGuild(dstGuildAddress);
            var dstValidatorAddress = dstGuild.ValidatorAddress;
            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            var validatorSrcDelegatee = validatorRepository.GetValidatorDelegatee(srcValidatorAddress);
            var validatorDstDelegatee = validatorRepository.GetValidatorDelegatee(dstValidatorAddress);
            var validatorDelegator = validatorRepository.GetValidatorDelegator(
                guildParticipantAddress, guild.Address);
            var bond = validatorRepository.GetBond(validatorSrcDelegatee, guildParticipantAddress);
            var share = bond.Share;
            validatorDelegator.Redelegate(validatorSrcDelegatee, validatorDstDelegatee, share, height);
            repository.UpdateWorld(validatorRepository.World);
            var guildShare = repository.GetBond(guild, guildParticipantAddress).Share;
            var guildRebondFAV = guild.Unbond(guildParticipant, guildShare, height);
            dstGuild.Bond(guildParticipant, guildRebondFAV, height);

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
