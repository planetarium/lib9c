#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Libplanet.Crypto;
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

        public static ValidatorRepository CreateValidatorDelegatee(
            this ValidatorRepository repository, PublicKey publicKey, BigInteger commissionPercentage)
        {
            var context = repository.ActionContext;
            var signer = context.Signer;

            if (!publicKey.Address.Equals(signer))
            {
                throw new ArgumentException("The public key does not match the signer.");
            }

            if (repository.TryGetValidatorDelegatee(context.Signer, out _))
            {
                throw new InvalidOperationException("The signer already has a validator delegatee.");
            }

            var validatorDelegatee = new ValidatorDelegatee(
                signer, publicKey, commissionPercentage, context.BlockIndex, repository);

            repository.SetValidatorDelegatee(validatorDelegatee);

            return repository;
        }
    }
}
