#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Model.Guild;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Module.ValidatorDelegation
{
    public static class ValidatorDelegatorModule
    {
        public static ValidatorRepository DelegateValidator(
            this ValidatorRepository repository,
            IActionContext context,
            Address validatorAddress,
            FungibleAssetValue fav)
            => DelegateValidator(repository, context, validatorAddress, validatorAddress, fav);


        public static ValidatorRepository DelegateValidator(
            this ValidatorRepository repository,
            IActionContext context,
            Address validatorAddress,
            Address rewardAddress,
            FungibleAssetValue fav)
        {
            var delegatorAddress = context.Signer;
            var validatorDelegator = repository.GetValidatorDelegator(
                delegatorAddress, rewardAddress);
            var validatorDelegatee = repository.GetValidatorDelegatee(validatorAddress);
            validatorDelegator.Delegate(validatorDelegatee, fav, context.BlockIndex);

            return repository;
        }

        public static ValidatorRepository UndelegateValidator(
            this ValidatorRepository repository,
            IActionContext context,
            Address validatorAddress,
            BigInteger share)
            => UndelegateValidator(repository, context, validatorAddress, validatorAddress, share);

        public static ValidatorRepository UndelegateValidator(
            this ValidatorRepository repository,
            IActionContext context,
            Address validatorAddress,
            Address rewardAddress,
            BigInteger share)
        {
            var delegatorAddress = context.Signer;
            var validatorDelegator = repository.GetValidatorDelegator(
                delegatorAddress, rewardAddress);
            var validatorDelegatee = repository.GetValidatorDelegatee(validatorAddress);
            validatorDelegator.Undelegate(validatorDelegatee, share, context.BlockIndex);

            return repository;
        }

         public static ValidatorRepository RedelegateValidator(
            this ValidatorRepository repository,
            IActionContext context,
            Address srcValidatorAddress,
            Address dstValidatorAddress,
            BigInteger share)
            => RedelegateValidator(repository, context, srcValidatorAddress, dstValidatorAddress, dstValidatorAddress, share);

        public static ValidatorRepository RedelegateValidator(
            this ValidatorRepository repository,
            IActionContext context,
            Address srcValidatorAddress,
            Address dstValidatorAddress,
            Address rewardAddress,
            BigInteger share)
        {
            var delegatorAddress = context.Signer;
            var validatorDelegator = repository.GetValidatorDelegator(
                delegatorAddress, rewardAddress);
            var srcValidatorDelegatee = repository.GetValidatorDelegatee(srcValidatorAddress);
            var dstValidatorDelegatee = repository.GetValidatorDelegatee(dstValidatorAddress);
            validatorDelegator.Redelegate(srcValidatorDelegatee, dstValidatorDelegatee, share, context.BlockIndex);

            return repository;
        }

         public static ValidatorRepository ClaimRewardValidator(
            this ValidatorRepository repository,
            IActionContext context,
            Address validatorAddress)
            => ClaimRewardValidator(repository, context, validatorAddress, validatorAddress);

        public static ValidatorRepository ClaimRewardValidator(
            this ValidatorRepository repository,
            IActionContext context,
            Address address,
            Address rewardAddress)
        {
            var delegatorAddress = context.Signer;
            var validatorDelegator = repository.GetValidatorDelegator(
                delegatorAddress, rewardAddress);
            var validatorDelegatee = repository.GetValidatorDelegatee(address);

            validatorDelegator.ClaimReward(validatorDelegatee, context.BlockIndex);

            return repository;
        }

        public static bool TryGetValidatorDelegator(
            this ValidatorRepository repository,
            IActionContext context,
            Address address,
            Address rewardAddress,
            [NotNullWhen(true)] out ValidatorDelegator? validatorDelegator)
        {
            try
            {
                validatorDelegator = repository.GetValidatorDelegator(
                    address, rewardAddress);
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
