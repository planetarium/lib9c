using Lib9c.Tests.Delegation;
using Libplanet.Crypto;
using Nekoyume.Delegation;

public sealed class DummyDelegatee : Delegatee<DummyDelegator, DummyDelegatee>
{
    public DummyDelegatee(Address address, IDelegationRepository repository)
        : base(address, repository)
    {
    }

    public DummyDelegatee(Address address, Address accountAddress, DummyRepository repository)
        : base(
                address,
                accountAddress,
                DelegationFixture.TestCurrency,
                DelegationFixture.TestCurrency,
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
