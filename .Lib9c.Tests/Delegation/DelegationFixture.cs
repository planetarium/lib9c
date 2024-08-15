namespace Lib9c.Tests.Delegation
{
    using Lib9c.Tests.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using Nekoyume.Delegation;

    public class DelegationFixture
    {
        public static readonly Currency TestCurrency = Currency.Uncapped("test-del", 5, null);
        public static readonly Address FixedPoolAddress = new Address("0x75b21EbC56e5dAc817A1128Fb05d45853183117c");

        public DelegationFixture()
        {
            var stateStore = new TrieStateStore(new MemoryKeyValueStore());
            var world = new World(
                new WorldBaseState(
                    stateStore.Commit(
                        stateStore.GetStateRoot(null).SetMetadata(
                            new TrieMetadata(BlockMetadata.CurrentProtocolVersion))),
                    stateStore));
            var context = new ActionContext()
            {
                BlockProtocolVersion = BlockMetadata.CurrentProtocolVersion,
            };

            Repository = new TestDelegationRepo(world, context);
            TestDelegator1 = new TestDelegator(new Address("0x0054E98312C47E7Fa0ABed45C23Fa187e31C373a"), Repository);
            TestDelegator2 = new TestDelegator(new Address("0x327CCff388255E9399207C3d5a09357D0BBc73dF"), Repository);
            TestDelegatee1 = new TestDelegatee(new Address("0x67A44E11506b8f0Bb625fEECccb205b33265Bb48"), Repository);
            TestDelegatee2 = new TestDelegatee(new Address("0xea1C4eedEfC99691DEfc6eF2753FAfa8C17F4584"), Repository);
            DummyDelegatee1 = new DummyDelegatee(new Address("0x67A44E11506b8f0Bb625fEECccb205b33265Bb48"), Repository);
            DummyDelegator1 = new DummyDelegator(new Address("0x0054E98312C47E7Fa0ABed45C23Fa187e31C373a"), Repository);
        }

        public TestDelegationRepo Repository { get; }

        public TestDelegator TestDelegator1 { get; }

        public TestDelegator TestDelegator2 { get; }

        public TestDelegatee TestDelegatee1 { get; }

        public TestDelegatee TestDelegatee2 { get; }

        public DummyDelegatee DummyDelegatee1 { get; }

        public DummyDelegator DummyDelegator1 { get; }
    }
}
