#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Module.ValidatorDelegation
{
    public static class ValidatorDelegatorModule
    {
        public static ValidatorRepository DelegateValidator(
            this ValidatorRepository repository,
            Address validatorAddress,
            FungibleAssetValue fav)
            => DelegateValidator(repository, validatorAddress, validatorAddress, fav);


        public static ValidatorRepository DelegateValidator(
            this ValidatorRepository repository,
            Address validatorAddress,
            Address rewardAddress,
            FungibleAssetValue fav)
        {
            var context = repository.ActionContext;
            var delegatorAddress = context.Signer;
            var validatorDelegator = repository.GetValidatorDelegator(
                delegatorAddress, rewardAddress);
            var validatorDelegatee = repository.GetValidatorDelegatee(validatorAddress);
            validatorDelegator.Delegate(validatorDelegatee, fav, context.BlockIndex);

            return repository;
        }

        public static ValidatorRepository UndelegateValidator(
            this ValidatorRepository repository,
            Address validatorAddress,
            BigInteger share)
            => UndelegateValidator(repository, validatorAddress, validatorAddress, share);

        public static ValidatorRepository UndelegateValidator(
            this ValidatorRepository repository,
            Address validatorAddress,
            Address rewardAddress,
            BigInteger share)
        {
            var context = repository.ActionContext;
            var delegatorAddress = context.Signer;
            var validatorDelegator = repository.GetValidatorDelegator(
                delegatorAddress, rewardAddress);
            var validatorDelegatee = repository.GetValidatorDelegatee(validatorAddress);
            validatorDelegator.Undelegate(validatorDelegatee, share, context.BlockIndex);

            return repository;
        }

         public static ValidatorRepository RedelegateValidator(
            this ValidatorRepository repository,
            Address srcValidatorAddress,
            Address dstValidatorAddress,
            BigInteger share)
            => RedelegateValidator(repository, srcValidatorAddress, dstValidatorAddress, dstValidatorAddress, share);

        public static ValidatorRepository RedelegateValidator(
            this ValidatorRepository repository,
            Address srcValidatorAddress,
            Address dstValidatorAddress,
            Address rewardAddress,
            BigInteger share)
        {
            var context = repository.ActionContext;
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
            Address validatorAddress)
            => ClaimRewardValidator(repository, validatorAddress, validatorAddress);

        public static ValidatorRepository ClaimRewardValidator(
            this ValidatorRepository repository,
            Address address,
            Address rewardAddress)
        {
            var context = repository.ActionContext;
            var delegatorAddress = context.Signer;
            var validatorDelegator = repository.GetValidatorDelegator(
                delegatorAddress, rewardAddress);
            var validatorDelegatee = repository.GetValidatorDelegatee(address);

            validatorDelegator.ClaimReward(validatorDelegatee, context.BlockIndex);

            return repository;
        }

        public static bool TryGetValidatorDelegator(
            this ValidatorRepository repository,
            Address address,
            Address rewardAddress,
            [NotNullWhen(true)] out ValidatorDelegator? validatorDelegator)
        {
            try
            {
                validatorDelegator = repository.GetValidatorDelegator(
                    address,
                    rewardAddress);
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
