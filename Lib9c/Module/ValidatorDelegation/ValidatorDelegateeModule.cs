#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Lib9c;
using Libplanet.Action;
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
            this ValidatorRepository repository, IActionContext context, PublicKey publicKey, BigInteger commissionPercentage)
        {
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
                signer, publicKey, Currencies.GuildGold, repository.World.GetGoldCurrency(), commissionPercentage, context.BlockIndex, repository);

            repository.SetValidatorDelegatee(validatorDelegatee);

            return repository;
        }
    }
}
