#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Lib9c;
using Libplanet.Action.State;
using Libplanet.Action;
using Nekoyume.Extensions;
using Nekoyume.Model.Guild;
using Nekoyume.TypedAddress;
using Libplanet.Crypto;
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
            var signer = new AgentAddress(repository.ActionContext.Signer);
            if (repository.GetJoinedGuild(signer) is not null)
            {
                throw new InvalidOperationException("The signer already has a guild.");
            }

            if (repository.TryGetGuild(guildAddress, out _))
            {
                throw new InvalidOperationException("Duplicated guild address. Please retry.");
            }

            var guild = new Model.Guild.Guild(
                guildAddress, signer, validatorAddress, repository.World.GetGoldCurrency(), repository);
            repository.SetGuild(guild);
            repository.JoinGuild(guildAddress, signer);

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
            var validatorDelegatee = validatorRepository.GetValidatorDelegatee(guild.ValidatorAddress);
            var bond = validatorRepository.GetBond(validatorDelegatee, signer);
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

        public static GuildRepository CollectRewardGuild(
            this GuildRepository repository,
            GuildAddress guildAddress)
        {
            var guild = repository.GetGuild(guildAddress);
            guild.CollectRewards(repository.ActionContext.BlockIndex);
            repository.SetGuild(guild);

            return repository;
        }
    }
}
