#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Bencodex.Types;
using Lib9c;
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

            if (repository.GetDelegator(target) is { } delegator
                && delegator.UnbondingRefs.Count > 0)
            {
                throw new InvalidOperationException(
                    $"The signer cannot join guild while unbonding");
            }

            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            if (validatorRepository.TryGetDelegatee(signer, out var _))
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
            if (srcGuild.Address == dstGuild.Address)
            {
                throw new InvalidOperationException("The signer cannot move to the same guild.");
            }

            var srcGuildDelegatee = repository.GetDelegatee(srcGuild.ValidatorAddress);
            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            var dstValidatorDelegatee = validatorRepository.GetDelegatee(dstGuild.ValidatorAddress);
            if (dstValidatorDelegatee.Tombstoned)
            {
                throw new InvalidOperationException("The validator of the guild to move to has been tombstoned.");
            }

            var guildParticipant2 = new GuildParticipant(guildParticipantAddress, dstGuildAddress, repository);
            var bond = repository.GetBond(srcGuildDelegatee, guildParticipantAddress);
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

            if (repository.GetDelegator(agentAddress).UnbondingRefs.Count > 0)
            {
                throw new InvalidOperationException(
                    $"The signer cannot leave guild while unbonding");
            }

            var height = repository.ActionContext.BlockIndex;
            var guildParticipant = repository.GetGuildParticipant(agentAddress);
            var delegatee = repository.GetDelegatee(guild.ValidatorAddress);
            var bond = repository.GetBond(delegatee, agentAddress);
            var share = bond.Share;

            if (bond.Share > 0)
            {
                guildParticipant.Undelegate(guild, share, height);
            }

            repository.RemoveGuildParticipant(agentAddress);
            repository.DecreaseGuildMemberCount(guild.Address);

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
    }
}
