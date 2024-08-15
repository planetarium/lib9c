namespace Lib9c.Tests.Delegation
{
    using Bencodex.Types;
    using Libplanet.Crypto;
    using Nekoyume.Delegation;

    public sealed class TestDelegator : Delegator<TestDelegatee, TestDelegator>
    {
        public TestDelegator(Address address, IDelegationRepository repo)
            : base(address, repo)
        {
        }

        public TestDelegator(Address address, IValue bencoded, IDelegationRepository repo)
            : base(address, bencoded, repo)
        {
        }
    }
}
