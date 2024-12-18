namespace Lib9c.Tests.Delegation
{
    using Libplanet.Crypto;
    using Nekoyume.Delegation;

    public sealed class TestDelegator : Delegator<TestRepository, TestDelegatee, TestDelegator>
    {
        public TestDelegator(Address address, TestRepository repo)
            : base(address, repo)
        {
        }

        public TestDelegator(Address address, Address accountAddress, TestRepository repo)
            : base(address, accountAddress, address, address, repo)
        {
        }
    }
}
