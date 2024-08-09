namespace Lib9c.Tests.Delegation
{
    using Bencodex.Types;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Delegation;

    public sealed class TestDelegatee : Delegatee<TestDelegator, TestDelegatee>
    {
        public TestDelegatee(Address address)
            : base(address)
        {
        }

        public TestDelegatee(Address address, IValue bencoded)
            : base(address, bencoded)
        {
        }

        public override Currency Currency => DelegationFixture.TestCurrency;

        public override Currency RewardCurrency => DelegationFixture.TestCurrency;

        public override long UnbondingPeriod => 3;

        public override Address PoolAddress => DeriveAddress(PoolId);

        public override byte[] DelegateeId => new byte[] { 0x01 };

        public override int MaxUnbondLockInEntries => 5;

        public override int MaxRebondGraceEntries => 5;
    }
}
