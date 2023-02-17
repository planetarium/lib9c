namespace Lib9c.Tests.Action
{
    using System;
    using Bencodex.Types;
    using Lib9c.Tests.Fixture.States;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class AccountStateDeltaExtensionsTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AgentState _agentState;
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;

        public AccountStateDeltaExtensionsTest()
        {
            _agentAddress = default;
            _avatarAddress = Addresses.GetAvatarAddress(_agentAddress, 0);
            _agentState = new AgentState(_agentAddress);
            _agentState.avatarAddresses[0] = _avatarAddress;
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
        }

        [Theory]
        [InlineData(0, 0, 0, typeof(InvalidClaimException))]
        [InlineData(1, 1, 100, null)]
        [InlineData(2, 2, 200, null)]
        public void SetWorldBossKillReward(
            int level,
            int expectedRune,
            int expectedCrystal,
            Type exc)
        {
            IAccountStateDelta states = new State();
            var rewardInfoAddress = new PrivateKey().ToAddress();
            var rewardRecord = new WorldBossKillRewardRecord();
            for (int i = 0; i < level; i++)
            {
                rewardRecord[i] = false;
            }

            states = states.SetState(rewardInfoAddress, rewardRecord.Serialize());

            var random = new TestRandom();
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var runeSheet = tableSheets.RuneSheet;
            var runeCurrency = RuneHelper.ToCurrency(runeSheet[10001], 0, null);
            var avatarAddress = new PrivateKey().ToAddress();
            var bossState = new WorldBossState(
                tableSheets.WorldBossListSheet[1],
                tableSheets.WorldBossGlobalHpSheet[1]
            );
            var bossId = bossState.Id;
            var runeWeightSheet = new RuneWeightSheet();
            runeWeightSheet.Set($@"id,boss_id,rank,rune_id,weight
1,{bossId},0,10001,100
");
            var killRewardSheet = new WorldBossKillRewardSheet();
            killRewardSheet.Set($@"id,boss_id,rank,rune_min,rune_max,crystal
1,{bossId},0,1,1,100
");

            if (exc is null)
            {
                var nextState = states.SetWorldBossKillReward(
                    rewardInfoAddress,
                    rewardRecord,
                    0,
                    bossState,
                    runeWeightSheet,
                    killRewardSheet,
                    runeSheet,
                    random,
                    avatarAddress,
                    _agentAddress);
                Assert.Equal(
                    expectedRune * runeCurrency,
                    nextState.GetBalance(avatarAddress, runeCurrency));
                Assert.Equal(
                    expectedCrystal * CrystalCalculator.CRYSTAL,
                    nextState.GetBalance(_agentAddress, CrystalCalculator.CRYSTAL));
                var nextRewardInfo =
                    new WorldBossKillRewardRecord((List)nextState.GetState(rewardInfoAddress));
                Assert.All(nextRewardInfo, kv => Assert.True(kv.Value));
            }
            else
            {
                Assert.Throws(
                    exc,
                    () => states.SetWorldBossKillReward(
                        rewardInfoAddress,
                        rewardRecord,
                        0,
                        bossState,
                        runeWeightSheet,
                        killRewardSheet,
                        runeSheet,
                        random,
                        avatarAddress,
                        _agentAddress)
                );
            }
        }

        [Fact]
        public void SetState()
        {
            IAccountStateDelta states = new State();
            var addr = new PrivateKey().ToAddress();

            states = states.SetState(addr, (Text)"foo", (Integer)1, (Text)"data");
            var serialized = states.GetVersionedState(
                addr,
                out var moniker,
                out var version);
            Assert.NotNull(serialized);
            Assert.NotNull(moniker);
            Assert.True(version.HasValue);
            Assert.Equal("foo", moniker);
            Assert.Equal(1u, (uint)version);
            Assert.Equal((Text)"data", serialized);

            states = states.SetState(addr, "foo", 2, Null.Value);
            serialized = states.GetVersionedState(
                addr,
                out moniker,
                out version);
            Assert.NotNull(serialized);
            Assert.NotNull(moniker);
            Assert.True(version.HasValue);
            Assert.Equal("foo", moniker);
            Assert.Equal(2u, (uint)version);
            Assert.Equal(Null.Value, serialized);

            states = states.SetState(addr, "foo", 3, new TestStateV1(100));
            serialized = states.GetVersionedState(
                addr,
                out moniker,
                out version);
            Assert.NotNull(serialized);
            Assert.NotNull(moniker);
            Assert.True(version.HasValue);
            Assert.Equal("foo", moniker);
            Assert.Equal(3u, (uint)version);
            Assert.Equal(100, (int)(Integer)serialized);

            states = states.SetState(addr, new TestStateV1(1000));
            serialized = states.GetVersionedState(
                addr,
                out moniker,
                out version);
            Assert.NotNull(serialized);
            Assert.NotNull(moniker);
            Assert.True(version.HasValue);
            Assert.Equal("test", moniker);
            Assert.Equal(1u, (uint)version);
            Assert.Equal(1000, (int)(Integer)serialized);
        }
    }
}
