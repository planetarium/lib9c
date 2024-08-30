namespace Lib9c.Tests.Delegation
{
    using System.Numerics;
    using Bencodex.Types;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Delegation;

    public sealed class TestDelegatee : Delegatee<TestDelegator, TestDelegatee>
    {
        public TestDelegatee(Address address, IDelegationRepository repository)
            : base(address, repository)
        {
        }

        public TestDelegatee(Address address, IValue bencoded, IDelegationRepository repository)
            : base(address, bencoded, repository)
        {
        }

        public override Currency DelegationCurrency => DelegationFixture.TestCurrency;

        public override Currency RewardCurrency => DelegationFixture.TestCurrency;

        public override long UnbondingPeriod => 3;

        public override Address DelegationPoolAddress => DeriveAddress(PoolId);

        public override byte[] DelegateeId => new byte[] { 0x01 };

        public override int MaxUnbondLockInEntries => 5;

        public override int MaxRebondGraceEntries => 5;

        public override BigInteger SlashFactor => 1;
    }
}
