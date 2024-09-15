#nullable enable
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Delegation;

namespace Nekoyume.ValidatorDelegation
{
    public sealed class ValidatorRepository : DelegationRepository
    {
        private readonly Address validatorListAddress = Addresses.ValidatorList;

        private IAccount _validatorList;

        public ValidatorRepository(IWorld world, IActionContext context)
            : base(
                  world,
                  context,
                  Addresses.ValidatorDelegatee,
                  Addresses.ValidatorDelegator,
                  Addresses.ValidatorDelegateeMetadata,
                  Addresses.ValidatorDelegatorMetadata,
                  Addresses.ValidatorBond,
                  Addresses.ValidatorUnbondLockIn,
                  Addresses.ValidatorRebondGrace,
                  Addresses.ValidatorUnbondingSet,
                  Addresses.ValidatorLumpSumRewardsRecord)
        {
            _validatorList = world.GetAccount(validatorListAddress);
        }

        public override IWorld World => base.World
            .SetAccount(validatorListAddress, _validatorList);

        public ValidatorDelegatee GetValidatorDelegatee(Address address)
            => delegateeAccount.GetState(address) is IValue bencoded
                ? new ValidatorDelegatee(
                    address,
                    bencoded,
                    this)
                : throw new FailedLoadStateException("Delegatee does not exist.");

        public override IDelegatee GetDelegatee(Address address)
            => GetValidatorDelegatee(address);

        public ValidatorDelegator GetValidatorDelegator(Address address)
        {
            try
            {
                return new ValidatorDelegator(address, this);
            }
            catch (FailedLoadStateException)
            {
                return new ValidatorDelegator(address, address, this);
            }
        }

        public override IDelegator GetDelegator(Address address)
            => GetValidatorDelegator(address);

        public ValidatorList GetValidatorList()
        {
            IValue? value = _validatorList.GetState(ValidatorList.Address);
            return value is IValue bencoded
                ? new ValidatorList(bencoded)
                : new ValidatorList();
        }

        public void SetValidatorDelegatee(ValidatorDelegatee validatorDelegatee)
        {
            delegateeAccount = delegateeAccount.SetState(
                validatorDelegatee.Address, validatorDelegatee.Bencoded);
            SetDelegateeMetadata(validatorDelegatee.Metadata);
        }

        public override void SetDelegatee(IDelegatee delegatee)
            => SetValidatorDelegatee((ValidatorDelegatee)delegatee);

        public void SetValidatorDelegator(ValidatorDelegator validatorDelegator)
        {
            SetDelegatorMetadata(validatorDelegator.Metadata);
        }

        public override void SetDelegator(IDelegator delegator)
            => SetValidatorDelegator((ValidatorDelegator)delegator);

        public void SetValidatorList(ValidatorList validatorList)
        {
            _validatorList = _validatorList.SetState(
                ValidatorList.Address, validatorList.Bencoded);
        }

        public override void UpdateWorld(IWorld world)
        {
            base.UpdateWorld(world);
            _validatorList = world.GetAccount(validatorListAddress);
        }
    }
}