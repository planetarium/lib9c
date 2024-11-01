#nullable enable
namespace Nekoyume.Delegation
{
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;

    public class DummyRepository
        : DelegationRepository<DummyRepository, DummyDelegatee, DummyDelegator>
    {
        public DummyRepository(IWorld world, IActionContext context)
            : base(
                  world: world,
                  actionContext: context,
                  delegateeAccountAddress: new Address("1000000000000000000000000000000000000000"),
                  delegatorAccountAddress: new Address("1000000000000000000000000000000000000001"),
                  delegateeMetadataAccountAddress: new Address("0000000000000000000000000000000000000002"),
                  delegatorMetadataAccountAddress: new Address("0000000000000000000000000000000000000003"),
                  bondAccountAddress: new Address("0000000000000000000000000000000000000004"),
                  unbondLockInAccountAddress: new Address("0000000000000000000000000000000000000005"),
                  rebondGraceAccountAddress: new Address("0000000000000000000000000000000000000006"),
                  unbondingSetAccountAddress: new Address("0000000000000000000000000000000000000007"),
                  lumpSumRewardRecordAccountAddress: new Address("0000000000000000000000000000000000000008"))
        {
        }

        public override DummyDelegatee GetDelegatee(Address address)
        {
            try
            {
                return new DummyDelegatee(address, this);
            }
            catch (FailedLoadStateException)
            {
                return new DummyDelegatee(address, DelegateeAccountAddress, this);
            }
        }

        public override DummyDelegator GetDelegator(Address address)
        {
            try
            {
                return new DummyDelegator(address, this);
            }
            catch (FailedLoadStateException)
            {
                return new DummyDelegator(address, DelegatorAccountAddress, this);
            }
        }

        public override void SetDelegatee(DummyDelegatee delegatee)
            => SetDelegateeMetadata(delegatee.Metadata);

        public override void SetDelegator(DummyDelegator delegator)
            => SetDelegatorMetadata(delegator.Metadata);
    }
}
