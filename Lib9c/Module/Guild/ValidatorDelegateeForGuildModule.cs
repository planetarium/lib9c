#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Lib9c;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.Guild;
using Nekoyume.Module.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;


namespace Nekoyume.Module.Guild
{
    public static class ValidatorDelegateeForGuildModule
    {
        public static bool TryGetValidatorDelegateeForGuild(
            this GuildRepository repository,
            Address address,
            [NotNullWhen(true)] out ValidatorDelegateeForGuildParticipant? validatorDelegateeForGuildParticipant)
        {
            try
            {
                validatorDelegateeForGuildParticipant = repository.GetValidatorDelegateeForGuildParticipant(address);
                return true;
            }
            catch
            {
                validatorDelegateeForGuildParticipant = null;
                return false;
            }
        }

        public static GuildRepository CreateValidatorDelegateeForGuildParticipant(
            this GuildRepository repository)
        {
            var context = repository.ActionContext;
            var signer = context.Signer;

            if (repository.TryGetValidatorDelegateeForGuild(context.Signer, out _))
            {
                throw new InvalidOperationException("The signer already has a validator delegatee for guild.");
            }

            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            if (!validatorRepository.TryGetValidatorDelegatee(context.Signer, out _))
            {
                throw new InvalidOperationException("The signer does not have a validator delegatee.");
            }

            var validatorDelegateeForGuildParticipant = new ValidatorDelegateeForGuildParticipant(
                signer,
                new Currency[] { repository.World.GetGoldCurrency() },
                repository);

            repository.SetValidatorDelegateeForGuildParticipant(validatorDelegateeForGuildParticipant);

            return repository;
        }
    }
}
