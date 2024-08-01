namespace Lib9c.Tests.Delegation
{
    using Bencodex.Types;
    using Libplanet.Crypto;
    using Nekoyume.Delegation;

    public sealed class TestDelegator : Delegator<TestDelegatee, TestDelegator>
    {
        public TestDelegator(Address address)
            : base(address)
        {
        }

        public TestDelegator(Address address, IValue bencoded)
            : base(address, bencoded)
        {
        }
    }
}
