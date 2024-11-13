#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.Guild;
using Nekoyume.Module.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;


namespace Nekoyume.Module.Guild
{
    public static class GuildDelegateeModule
    {
        public static bool TryGetGuildDelegatee(
            this GuildRepository repository,
            Address address,
            [NotNullWhen(true)] out GuildDelegatee? validatorDelegateeForGuildParticipant)
        {
            try
            {
                validatorDelegateeForGuildParticipant = repository.GetGuildDelegatee(address);
                return true;
            }
            catch
            {
                validatorDelegateeForGuildParticipant = null;
                return false;
            }
        }

        public static GuildDelegatee CreateGuildDelegatee(
            this GuildRepository repository,
            Address address)
        {
            if (repository.TryGetGuildDelegatee(address, out _))
            {
                throw new InvalidOperationException("The signer already has a validator delegatee for guild.");
            }

            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            if (!validatorRepository.TryGetValidatorDelegatee(address, out _))
            {
                throw new InvalidOperationException("The signer does not have a validator delegatee.");
            }

            var guildDelegatee = new GuildDelegatee(
                address,
                new Currency[] { repository.World.GetGoldCurrency() },
                repository);

            repository.SetGuildDelgatee(guildDelegatee);

            return guildDelegatee;
        }
    }
}
