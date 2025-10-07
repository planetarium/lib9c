#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Lib9c.Model.Guild;
using Lib9c.Module.ValidatorDelegation;
using Lib9c.ValidatorDelegation;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Module.Guild
{
    public static class GuildDelegateeModule
    {
        public static bool TryGetDelegatee(
            this GuildRepository repository,
            Address address,
            [NotNullWhen(true)] out GuildDelegatee? guildDelegatee)
        {
            try
            {
                guildDelegatee = repository.GetDelegatee(address);
                return true;
            }
            catch
            {
                guildDelegatee = null;
                return false;
            }
        }

        public static GuildDelegatee CreateDelegatee(
            this GuildRepository repository,
            Address address)
        {
            if (repository.TryGetDelegatee(address, out _))
            {
                throw new InvalidOperationException("The signer already has a validator delegatee for guild.");
            }

            var validatorRepository = new ValidatorRepository(repository.World, repository.ActionContext);
            if (!validatorRepository.TryGetDelegatee(address, out _))
            {
                throw new InvalidOperationException("The signer does not have a validator delegatee.");
            }

            var guildDelegatee = new GuildDelegatee(
                address,
                new Currency[] { repository.World.GetGoldCurrency() },
                repository);

            repository.SetDelegatee(guildDelegatee);

            return guildDelegatee;
        }
    }
}
