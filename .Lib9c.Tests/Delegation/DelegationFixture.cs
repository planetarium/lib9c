namespace Lib9c.Tests.Delegation
{
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Delegation;

    public class DelegationFixture
    {
        public static readonly Currency TestCurrency = Currency.Uncapped("test-del", 5, null);
        public static readonly Address FixedPoolAddress = new Address("0x75b21EbC56e5dAc817A1128Fb05d45853183117c");

        public DelegationFixture()
        {
            TestDelegator1 = new TestDelegator(new Address("0x0054E98312C47E7Fa0ABed45C23Fa187e31C373a"));
            TestDelegator2 = new TestDelegator(new Address("0x327CCff388255E9399207C3d5a09357D0BBc73dF"));
            TestDelegatee1 = new TestDelegatee(new Address("0x67A44E11506b8f0Bb625fEECccb205b33265Bb48"));
            TestDelegatee2 = new TestDelegatee(new Address("0xea1C4eedEfC99691DEfc6eF2753FAfa8C17F4584"));
            DummyDelegatee1 = new DummyDelegatee(new Address("0x67A44E11506b8f0Bb625fEECccb205b33265Bb48"));
            DummyDelegator1 = new DummyDelegator(new Address("0x0054E98312C47E7Fa0ABed45C23Fa187e31C373a"));
            Bond1To1 = new Bond(TestDelegatee1.BondAddress(TestDelegator1.Address));
            Bond2To1 = new Bond(TestDelegatee1.BondAddress(TestDelegator2.Address));
            Bond1To2 = new Bond(TestDelegatee2.BondAddress(TestDelegator1.Address));
            Unbond1To1 = new UnbondLockIn(TestDelegatee1.BondAddress(TestDelegator1.Address), 10);
            Unbond2To1 = new UnbondLockIn(TestDelegatee1.BondAddress(TestDelegator2.Address), 10);
            Unbond1To2 = new UnbondLockIn(TestDelegatee2.BondAddress(TestDelegator1.Address), 10);
            Rebond1To1 = new RebondGrace(TestDelegatee1.BondAddress(TestDelegator1.Address), 10);
            Rebond2To1 = new RebondGrace(TestDelegatee1.BondAddress(TestDelegator2.Address), 10);
            Rebond1To2 = new RebondGrace(TestDelegatee2.BondAddress(TestDelegator1.Address), 10);
            UnbondingSet = new UnbondingSet();
        }

        public TestDelegator TestDelegator1 { get; }

        public TestDelegator TestDelegator2 { get; }

        public TestDelegatee TestDelegatee1 { get; }

        public TestDelegatee TestDelegatee2 { get; }

        public DummyDelegatee DummyDelegatee1 { get; }

        public DummyDelegator DummyDelegator1 { get; }

        public Bond Bond1To1 { get; }

        public Bond Bond2To1 { get; }

        public Bond Bond1To2 { get; }

        public UnbondLockIn Unbond1To1 { get; }

        public UnbondLockIn Unbond2To1 { get; }

        public UnbondLockIn Unbond1To2 { get; }

        public RebondGrace Rebond1To1 { get; }

        public RebondGrace Rebond2To1 { get; }

        public RebondGrace Rebond1To2 { get; }

        public UnbondingSet UnbondingSet { get; }
    }
}
