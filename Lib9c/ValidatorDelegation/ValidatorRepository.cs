#nullable enable
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Delegation;
using Nekoyume.Model.Guild;

namespace Nekoyume.ValidatorDelegation
{
    public sealed class ValidatorRepository
        : DelegationRepository<ValidatorRepository, ValidatorDelegatee, ValidatorDelegator>
    {
        private readonly Address validatorListAddress = Addresses.ValidatorList;

        private IAccount _validatorListAccount;

        public ValidatorRepository(GuildRepository repository)
            : this(repository.World, repository.ActionContext)
        {
        }

        public ValidatorRepository(IWorld world, IActionContext actionContext)
            : base(
                  world,
                  actionContext,
                  Addresses.ValidatorDelegatee,
                  Addresses.ValidatorDelegator,
                  Addresses.ValidatorDelegateeMetadata,
                  Addresses.ValidatorDelegatorMetadata,
                  Addresses.ValidatorBond,
                  Addresses.ValidatorUnbondLockIn,
                  Addresses.ValidatorRebondGrace,
                  Addresses.ValidatorUnbondingSet,
                  Addresses.ValidatorRewardBase,
                  Addresses.ValidatorLumpSumRewardsRecord)
        {
            _validatorListAccount = world.GetAccount(validatorListAddress);
        }

        public override IWorld World => base.World
            .SetAccount(validatorListAddress, _validatorListAccount);

        public override ValidatorDelegatee GetDelegatee(Address address)
            => delegateeAccount.GetState(address) is IValue bencoded
                ? new ValidatorDelegatee(
                    address,
                    bencoded,
                    this)
                : throw new FailedLoadStateException("Delegatee does not exist.");

        public override ValidatorDelegator GetDelegator(Address address)
        {
            try
            {
                return new ValidatorDelegator(address, this);
            }
            catch (FailedLoadStateException)
            {
                return new ValidatorDelegator(
                    address,
                    address,
                    this);
            }
        }

        public ValidatorList GetValidatorList()
        {
            IValue? value = _validatorListAccount.GetState(ValidatorList.Address);
            return value is IValue bencoded
                ? new ValidatorList(bencoded)
                : new ValidatorList();
        }

        // public override void SetDelegatee(ValidatorDelegatee delegatee)
        // {
        //     delegateeAccount = delegateeAccount.SetState(
        //         delegatee.Address, delegatee.Bencoded);
        //     SetDelegateeMetadata(delegatee.Metadata);
        // }

        // public override void SetDelegator(ValidatorDelegator delegator)
        // {
        //     SetDelegatorMetadata(delegator.Metadata);
        // }

        public void SetValidatorList(ValidatorList validatorList)
        {
            _validatorListAccount = _validatorListAccount.SetState(
                ValidatorList.Address, validatorList.Bencoded);
        }

        public void SetCommissionPercentage(Address address, BigInteger commissionPercentage, long height)
        {
            ValidatorDelegatee validatorDelegatee = GetDelegatee(address);
            validatorDelegatee.SetCommissionPercentage(commissionPercentage, height);
            SetDelegatee(validatorDelegatee);
        }

        public override void UpdateWorld(IWorld world)
        {
            base.UpdateWorld(world);
            _validatorListAccount = world.GetAccount(validatorListAddress);
        }
    }
}
