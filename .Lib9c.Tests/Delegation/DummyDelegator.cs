using Libplanet.Crypto;
using Nekoyume.Delegation;

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
