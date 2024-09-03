#nullable enable
namespace Nekoyume.Delegation
{
    using Lib9c.Tests.Delegation;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;

    public class TestRepository : DelegationRepository
    {
        public TestRepository(IWorld world, IActionContext context)
            : base(
                  world: world,
                  context: context,
                  delegateeAccountAddress: new Address("0000000000000000000000000000000000000000"),
                  delegatorAccountAddress: new Address("0000000000000000000000000000000000000001"),
                  delegateeMetadataAccountAddress: new Address("0000000000000000000000000000000000000002"),
                  delegatorMetadataAccountAddress: new Address("0000000000000000000000000000000000000003"),
                  bondAccountAddress: new Address("0000000000000000000000000000000000000004"),
                  unbondLockInAccountAddress: new Address("0000000000000000000000000000000000000005"),
                  rebondGraceAccountAddress: new Address("0000000000000000000000000000000000000006"),
                  unbondingSetAccountAddress: new Address("0000000000000000000000000000000000000007"),
                  lumpSumRewardRecordAccountAddress: new Address("0000000000000000000000000000000000000008"))
        {
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

        public override void SetDelegatee(IDelegatee delegatee)
            => SetDelegateeMetadata(((TestDelegatee)delegatee).Metadata);

        public override void SetDelegator(IDelegator delegator)
            => SetDelegatorMetadata(((TestDelegator)delegator).Metadata);

        public void MintAsset(Address recipient, FungibleAssetValue value)
            => previousWorld = previousWorld.MintAsset(actionContext, recipient, value);
    }
}
