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
        public static bool TryGetValidatorDelegatee(
            this ValidatorRepository repository,
            Address address,
            [NotNullWhen(true)] out ValidatorDelegatee? validatorDelegatee)
        {
            try
            {
                validatorDelegatee = repository.GetValidatorDelegatee(address);
                return true;
            }
            catch
            {
                validatorDelegatee = null;
                return false;
            }
        }

        public static ValidatorDelegatee CreateValidatorDelegatee(
            this ValidatorRepository repository, PublicKey publicKey, BigInteger commissionPercentage)
        {
            var context = repository.ActionContext;

            if (repository.TryGetValidatorDelegatee(publicKey.Address, out _))
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

            repository.SetValidatorDelegatee(validatorDelegatee);

            return validatorDelegatee;
        }
    }
}
