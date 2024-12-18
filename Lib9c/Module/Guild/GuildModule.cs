#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Model.Guild;
using Nekoyume.Module.ValidatorDelegation;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Module.Guild
{
    public static class GuildModule
    {
        public static GuildRepository GetGuildRepository(this IWorld world, IActionContext context)
            => new GuildRepository(world, context);

        public static bool TryGetGuild(
            this GuildRepository repository,
            GuildAddress guildAddress,
            [NotNullWhen(true)] out Model.Guild.Guild? guild)
        {
            try
            {
                guild = repository.GetGuild(guildAddress);
                return true;
            }
            catch
            {
                guild = null;
                return false;
            }
        }

        public static GuildRepository MakeGuild(
            this GuildRepository repository,
            GuildAddress guildAddress,
            Address validatorAddress)
        {
            var signer = repository.ActionContext.Signer;
            if (repository.GetJoinedGuild(new AgentAddress(signer)) is not null)
            {
                throw new InvalidOperationException("The signer already has a guild.");
            }

            if (repository.TryGetGuild(guildAddress, out _))
            {
                throw new InvalidOperationException("Duplicated guild address. Please retry.");
            }

            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            if (!validatorRepository.TryGetValidatorDelegatee(validatorAddress, out _))
            {
                throw new InvalidOperationException("The validator does not exist.");
            }

            if (validatorRepository.TryGetValidatorDelegatee(signer, out var _))
            {
                throw new InvalidOperationException("Validator cannot make a guild.");
            }

            var guildMasterAddress = new AgentAddress(signer);
            var guild = new Model.Guild.Guild(
                guildAddress, guildMasterAddress, validatorAddress, repository);
            repository.SetGuild(guild);
            repository.JoinGuild(guildAddress, guildMasterAddress);

            return repository;
        }

        public static GuildRepository RemoveGuild(
            this GuildRepository repository)
        {
            var signer = new AgentAddress(repository.ActionContext.Signer);
            if (repository.GetJoinedGuild(signer) is not { } guildAddress)
            {
                throw new InvalidOperationException("The signer does not join any guild.");
            }

            if (!repository.TryGetGuild(guildAddress, out var guild))
            {
                throw new InvalidOperationException("There is no such guild.");
            }

            if (guild.GuildMasterAddress != signer)
            {
                throw new InvalidOperationException("The signer is not a guild master.");
            }

            if (repository.GetGuildMemberCount(guildAddress) > 1)
            {
                throw new InvalidOperationException("There are remained participants in the guild.");
            }

            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            var validatorDelegatee = validatorRepository.GetDelegatee(guild.ValidatorAddress);
            var bond = validatorRepository.GetBond(validatorDelegatee, guild.Address);
            if (bond.Share > 0)
            {
                throw new InvalidOperationException("The signer has a bond with the validator.");
            }

            repository.RemoveGuildParticipant(signer);
            repository.DecreaseGuildMemberCount(guild.Address);
            repository.UpdateWorld(
                repository.World.MutateAccount(
                    Addresses.Guild, account => account.RemoveState(guildAddress)));
            repository.RemoveBanList(guildAddress);

            return repository;
        }
    }
}
