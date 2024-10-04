namespace Lib9c.Tests.Delegation
{
    using Libplanet.Crypto;
    using Nekoyume.Delegation;

    public sealed class TestDelegatee : Delegatee<TestDelegator, TestDelegatee>
    {
        public TestDelegatee(Address address, TestRepository repository)
            : base(address, repository)
        {
        }

        public TestDelegatee(Address address, Address accountAddress, TestRepository repository)
            : base(
                  address,
                  accountAddress,
                  DelegationFixture.TestCurrency,
                  DelegationFixture.TestCurrency,
                  address,
                  DelegationFixture.FixedPoolAddress,
                  DelegationFixture.FixedPoolAddress,
                  3,
                  5,
                  5,
                  repository)
        {
        }
    }
}
