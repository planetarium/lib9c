namespace Lib9c.Tests.Delegation
{
#nullable enable
    using Lib9c.Action;
    using Lib9c.Delegation;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;

    public class TestRepository : DelegationRepository<TestRepository, TestDelegatee, TestDelegator>
    {
        private readonly IActionContext _context;

        public TestRepository(IWorld world, IActionContext context)
            : base(
                  world: world,
                  actionContext: context,
                  delegateeAccountAddress: new Address("0000000000000000000000000000000000000000"),
                  delegatorAccountAddress: new Address("0000000000000000000000000000000000000001"),
                  delegateeMetadataAccountAddress: new Address("0000000000000000000000000000000000000002"),
                  delegatorMetadataAccountAddress: new Address("0000000000000000000000000000000000000003"),
                  bondAccountAddress: new Address("0000000000000000000000000000000000000004"),
                  unbondLockInAccountAddress: new Address("0000000000000000000000000000000000000005"),
                  rebondGraceAccountAddress: new Address("0000000000000000000000000000000000000006"),
                  unbondingSetAccountAddress: new Address("0000000000000000000000000000000000000007"),
                  rewardBaseAccountAddress: new Address("0000000000000000000000000000000000000008"),
                  lumpSumRewardRecordAccountAddress: new Address("0000000000000000000000000000000000000009"))
        {
            _context = context;
        }

        public override TestDelegatee GetDelegatee(Address address)
        {
            try
            {
                return new TestDelegatee(address, this);
            }
            catch (FailedLoadStateException)
            {
                return new TestDelegatee(address, DelegateeAccountAddress, this);
            }
        }

        public override TestDelegator GetDelegator(Address address)
        {
            try
            {
                return new TestDelegator(address, this);
            }
            catch (FailedLoadStateException)
            {
                return new TestDelegator(address, DelegatorAccountAddress, this);
            }
        }

        public override void SetDelegatee(TestDelegatee delegatee)
            => SetDelegateeMetadata(delegatee.Metadata);

        public override void SetDelegator(TestDelegator delegator)
            => SetDelegatorMetadata(delegator.Metadata);

        public void MintAsset(Address recipient, FungibleAssetValue value)
            => previousWorld = previousWorld.MintAsset(_context, recipient, value);
    }
}
