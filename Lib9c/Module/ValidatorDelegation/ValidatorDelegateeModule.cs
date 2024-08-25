#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Module.ValidatorDelegation
{
    public static class ValidatorDelegateeModule
    {
        public static ValidatorDelegatee GetValidatorDelegatee(this IWorldState worldState, Address address)
            => worldState.TryGetValidatorDelegatee(address, out var validatorDelegatee)
                ? validatorDelegatee
                : throw new FailedLoadStateException("There is no such validator delegatee");

        public static ValidatorDelegatee GetValidatorDelegatee(
            this IWorldState worldState, Address address, ValidatorRepository repository)
            => worldState.TryGetValidatorDelegatee(address, repository, out var validatorDelegatee)
                ? validatorDelegatee
                : throw new FailedLoadStateException("There is no such validator delegatee");

        public static bool TryGetValidatorDelegatee(
            this IWorldState worldState,
            Address address,
            [NotNullWhen(true)] out ValidatorDelegatee? validatorDelegatee)
        {
            try
            {
                var value = worldState.GetAccountState(Addresses.ValidatorDelegatee).GetState(address);
                if (!(value is List list))
                {
                    validatorDelegatee = null;
                    return false;
                }

                validatorDelegatee = new ValidatorDelegatee(address, list, worldState.GetGoldCurrency());
                return true;
            }
            catch
            {
                validatorDelegatee = null;
                return false;
            }
        }

        public static bool TryGetValidatorDelegatee(
            this IWorldState worldState,
            Address address,
            ValidatorRepository repository,
            [NotNullWhen(true)] out ValidatorDelegatee? validatorDelegatee)
        {
            try
            {
                var value = worldState.GetAccountState(Addresses.ValidatorDelegatee).GetState(address);
                if (!(value is List list))
                {
                    validatorDelegatee = null;
                    return false;
                }

                validatorDelegatee = new ValidatorDelegatee(address, list, worldState.GetGoldCurrency(), repository);
                return true;
            }
            catch
            {
                validatorDelegatee = null;
                return false;
            }
        }

        public static IWorld SetValidatorDelegatee(this IWorld world, ValidatorDelegatee validatorDelegatee)
            => world.MutateAccount(
                Addresses.ValidatorDelegatee,
                account => account.SetState(validatorDelegatee.Address, validatorDelegatee.Bencoded));

        public static IWorld CreateValidatorDelegatee(this IWorld world, IActionContext context, PublicKey publicKey)
        {
            var signer = context.Signer;

            if (!publicKey.Address.Equals(signer))
            {
                throw new ArgumentException("The public key does not match the signer.");
            }

            if (world.TryGetValidatorDelegatee(context.Signer, out _))
            {
                throw new InvalidOperationException("The signer already has a validator delegatee.");
            }

            return world.MutateAccount(
                Addresses.ValidatorDelegatee,
                account => account.SetState(
                    signer,
                    new ValidatorDelegatee(signer, publicKey, world.GetGoldCurrency()).Bencoded));
        }
    }
}
