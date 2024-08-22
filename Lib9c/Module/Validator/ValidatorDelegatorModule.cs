#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Extensions;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Module.Validator
{
    public static class ValidatorDelegatorModule
    {
        public static IWorld Delegate(
            this IWorld world,
            IActionContext context,
            Address address,
            FungibleAssetValue fav)
        {
            var repo = new ValidatorRepository(world, context);

            var validatorDelegator = world.GetValidatorDelegator(context.Signer, repo);
            var validatorDelegatee = world.GetValidatorDelegatee(address, repo);
            validatorDelegator.Delegate(validatorDelegatee, fav, context.BlockIndex);

            return repo.World
                .SetValidatorDelegatee(validatorDelegatee)
                .SetValidatorDelegator(validatorDelegator);
        }

        public static IWorld Undelegate(
            this IWorld world,
            IActionContext context,
            Address address,
            BigInteger share)
        {
            var repo = new ValidatorRepository(world, context);

            var validatorDelegator = world.GetValidatorDelegator(context.Signer, repo);
            var validatorDelegatee = world.GetValidatorDelegatee(address, repo);
            validatorDelegator.Undelegate(validatorDelegatee, share, context.BlockIndex);

            return repo.World
                .SetValidatorDelegatee(validatorDelegatee)
                .SetValidatorDelegator(validatorDelegator);
        }

        public static IWorld Redelegate(
            this IWorld world,
            IActionContext context,
            Address srcAddress,
            Address dstAddress,
            BigInteger share)
        {
            var repo = new ValidatorRepository(world, context);

            var validatorDelegator = world.GetValidatorDelegator(context.Signer, repo);
            var srcValidatorDelegatee = world.GetValidatorDelegatee(srcAddress, repo);
            var dstValidatorDelegatee = world.GetValidatorDelegatee(dstAddress, repo);
            validatorDelegator.Redelegate(srcValidatorDelegatee, dstValidatorDelegatee, share, context.BlockIndex);

            return repo.World
                .SetValidatorDelegatee(srcValidatorDelegatee)
                .SetValidatorDelegatee(dstValidatorDelegatee)
                .SetValidatorDelegator(validatorDelegator);
        }

        public static IWorld ClaimReward(
            this IWorld world,
            IActionContext context,
            Address address)
        {
            var repo = new ValidatorRepository(world, context);

            var validatorDelegator = world.GetValidatorDelegator(context.Signer, repo);
            var validatorDelegatee = world.GetValidatorDelegatee(address, repo);

            validatorDelegator.ClaimReward(validatorDelegatee, context.BlockIndex);

            return repo.World
                .SetValidatorDelegatee(validatorDelegatee)
                .SetValidatorDelegator(validatorDelegator);
        }

        public static ValidatorDelegator GetValidatorDelegator(this IWorldState worldState, Address address)
            => worldState.TryGetValidatorDelegator(address, out var validatorDelegator)
                ? validatorDelegator
                : new ValidatorDelegator(address);

        public static ValidatorDelegator GetValidatorDelegator(
            this IWorldState worldState, Address address, ValidatorRepository repository)
            => worldState.TryGetValidatorDelegator(address, repository, out var validatorDelegator)
                ? validatorDelegator
                : new ValidatorDelegator(address, repository);

        public static bool TryGetValidatorDelegator(
            this IWorldState worldState,
            Address address,
            [NotNullWhen(true)] out ValidatorDelegator? validatorDelegator)
        {
            try
            {
                var value = worldState.GetAccountState(Addresses.ValidatorDelegator).GetState(address);
                if (!(value is List list))
                {
                    validatorDelegator = null;
                    return false;
                }

                validatorDelegator = new ValidatorDelegator(address, list);
                return true;
            }
            catch
            {
                validatorDelegator = null;
                return false;
            }
        }

        public static bool TryGetValidatorDelegator(
            this IWorldState worldState,
            Address address,
            ValidatorRepository repository,
            [NotNullWhen(true)] out ValidatorDelegator? validatorDelegator)
        {
            try
            {
                var value = worldState.GetAccountState(Addresses.ValidatorDelegator).GetState(address);
                if (!(value is List list))
                {
                    validatorDelegator = null;
                    return false;
                }

                validatorDelegator = new ValidatorDelegator(address, list, repository);
                return true;
            }
            catch
            {
                validatorDelegator = null;
                return false;
            }
        }

        private static IWorld SetValidatorDelegator(this IWorld world, ValidatorDelegator validatorDelegator)
            => world.MutateAccount(
                Addresses.ValidatorDelegator,
                account => account.SetState(validatorDelegator.Address, validatorDelegator.Bencoded));
    }
}
