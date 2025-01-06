#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Lib9c;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Module.ValidatorDelegation
{
    public static class ValidatorDelegateeModule
    {
        public static bool TryGetDelegatee(
            this ValidatorRepository repository,
            Address address,
            [NotNullWhen(true)] out ValidatorDelegatee? validatorDelegatee)
        {
            try
            {
                validatorDelegatee = repository.GetDelegatee(address);
                return true;
            }
            catch
            {
                validatorDelegatee = null;
                return false;
            }
        }

        public static ValidatorDelegatee CreateDelegatee(
            this ValidatorRepository repository, PublicKey publicKey, BigInteger commissionPercentage)
        {
            var context = repository.ActionContext;

            if (repository.TryGetDelegatee(publicKey.Address, out _))
            {
                throw new InvalidOperationException("The public key already has a validator delegatee.");
            }

            var validatorDelegatee = new ValidatorDelegatee(
                publicKey.Address,
                publicKey,
                commissionPercentage,
                context.BlockIndex,
                new Currency[] { repository.World.GetGoldCurrency(), Currencies.Mead },
                repository);

            repository.SetDelegatee(validatorDelegatee);

            return validatorDelegatee;
        }
    }
}
