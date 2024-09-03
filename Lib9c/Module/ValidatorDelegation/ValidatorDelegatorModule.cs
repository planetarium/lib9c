#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Module.ValidatorDelegation
{
    public static class ValidatorDelegatorModule
    {
        public static ValidatorRepository DelegateValidator(
            this ValidatorRepository repository,
            IActionContext context,
            Address address,
            FungibleAssetValue fav)
        {
            var validatorDelegator = repository.GetValidatorDelegator(context.Signer);
            var validatorDelegatee = repository.GetValidatorDelegatee(address);
            validatorDelegator.Delegate(validatorDelegatee, fav, context.BlockIndex);

            return repository;
        }

        public static ValidatorRepository UndelegateValidator(
            this ValidatorRepository repository,
            IActionContext context,
            Address address,
            BigInteger share)
        {
            var validatorDelegator = repository.GetValidatorDelegator(context.Signer);
            var validatorDelegatee = repository.GetValidatorDelegatee(address);
            validatorDelegator.Undelegate(validatorDelegatee, share, context.BlockIndex);

            return repository;
        }

        public static ValidatorRepository RedelegateValidator(
            this ValidatorRepository repository,
            IActionContext context,
            Address srcAddress,
            Address dstAddress,
            BigInteger share)
        {
            var validatorDelegator = repository.GetValidatorDelegator(context.Signer);
            var srcValidatorDelegatee = repository.GetValidatorDelegatee(srcAddress);
            var dstValidatorDelegatee = repository.GetValidatorDelegatee(dstAddress);
            validatorDelegator.Redelegate(srcValidatorDelegatee, dstValidatorDelegatee, share, context.BlockIndex);

            return repository;
        }

        public static ValidatorRepository ClaimRewardValidator(
            this ValidatorRepository repository,
            IActionContext context,
            Address address)
        {
            var validatorDelegator = repository.GetValidatorDelegator(context.Signer);
            var validatorDelegatee = repository.GetValidatorDelegatee(address);

            validatorDelegator.ClaimReward(validatorDelegatee, context.BlockIndex);

            return repository;
        }

        public static bool TryGetValidatorDelegator(
            this ValidatorRepository repository,
            Address address,
            [NotNullWhen(true)] out ValidatorDelegator? validatorDelegator)
        {
            try
            {
                validatorDelegator = repository.GetValidatorDelegator(address);
                return true;
            }
            catch
            {
                validatorDelegator = null;
                return false;
            }
        }
    }
}
