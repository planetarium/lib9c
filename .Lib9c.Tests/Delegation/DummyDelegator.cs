using Libplanet.Crypto;
using Nekoyume.Delegation;

public sealed class DummyDelegator : Delegator<DummyDelegatee, DummyDelegator>
{
    public DummyDelegator(Address address, IDelegationRepository repository)
        : base(address, repository)
    {
    }
}
