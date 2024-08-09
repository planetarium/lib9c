using Lib9c.Tests.Delegation;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Delegation;

public sealed class DummyDelegatee : Delegatee<DummyDelegator, DummyDelegatee>
{
    public DummyDelegatee(Address address)
        : base(address)
    {
    }

    public override Currency Currency => DelegationFixture.TestCurrency;

    public override Currency RewardCurrency => DelegationFixture.TestCurrency;

    public override Address PoolAddress => DelegationFixture.FixedPoolAddress;

    public override long UnbondingPeriod => 3;

    public override byte[] DelegateeId => new byte[] { 0x02 };

    public override int MaxUnbondLockInEntries => 5;

    public override int MaxRebondGraceEntries => 5;
}
