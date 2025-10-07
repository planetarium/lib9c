using Lib9c.Delegation;
using Lib9c.Tests.Delegation;
using Libplanet.Crypto;

public sealed class DummyDelegator : Delegator<DummyRepository, DummyDelegatee, DummyDelegator>
{
    public DummyDelegator(Address address, DummyRepository repository)
        : base(address, repository)
    {
    }

    public DummyDelegator(Address address, Address accountAddress, DummyRepository repo)
        : base(address, accountAddress, address, address, repo)
    {
    }
}
