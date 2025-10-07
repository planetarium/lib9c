namespace Lib9c.Tests.Delegation
{
    using Lib9c.Delegation;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;

    public sealed class TestDelegatee : Delegatee<TestRepository, TestDelegatee, TestDelegator>
    {
        public TestDelegatee(Address address, TestRepository repository)
            : base(address, repository)
        {
        }

        public TestDelegatee(Address address, Address accountAddress, TestRepository repository)
            : base(
                  address,
                  accountAddress,
                  DelegationFixture.TestDelegationCurrency,
                  new Currency[] { DelegationFixture.TestRewardCurrency },
                  DelegationAddress.DelegationPoolAddress(address, accountAddress),
                  DelegationAddress.RewardPoolAddress(address, accountAddress),
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
