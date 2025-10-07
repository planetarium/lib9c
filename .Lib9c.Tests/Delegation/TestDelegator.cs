namespace Lib9c.Tests.Delegation
{
    using Lib9c.Delegation;
    using Libplanet.Crypto;

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
