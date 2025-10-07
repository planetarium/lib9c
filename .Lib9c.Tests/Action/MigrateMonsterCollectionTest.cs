namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Action;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Lib9c.Module.Guild;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class MigrateMonsterCollectionTest
    {
        private readonly Address _signer;
        private readonly Address _avatarAddress;
        private readonly IWorld _state;

        public MigrateMonsterCollectionTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _signer = default;
            _avatarAddress = _signer.Derive("avatar");
            _state = new World(MockUtil.MockModernWorldState);
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var rankingMapAddress = new PrivateKey().Address;
            var agentState = new AgentState(_signer);
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _signer,
                0,
                tableSheets.GetAvatarSheets(),
                rankingMapAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(currency);

            _state = _state
                .SetAgentState(_signer, agentState)
                .SetAvatarState(_avatarAddress, avatarState)
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            foreach (var (key, value) in sheets)
            {
                _state = _state
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void Execute_ThrowsIfAlreadyStakeStateExists()
        {
            var monsterCollectionAddress = MonsterCollectionState.DeriveAddress(_signer, 0);
            var monsterCollectionState = new MonsterCollectionState(
                monsterCollectionAddress, 1, 0);
            Address stakeStateAddress = LegacyStakeState.DeriveAddress(_signer);
            var states = _state.SetLegacyState(
                    stakeStateAddress, new LegacyStakeState(stakeStateAddress, 0).SerializeV2())
                .SetLegacyState(monsterCollectionAddress, monsterCollectionState.Serialize());
            var action = new MigrateMonsterCollection(_avatarAddress);
            Assert.Throws<InvalidOperationException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = states,
                        Signer = _signer,
                        BlockIndex = 0,
                        RandomSeed = 0,
                    }));
        }

        [Fact]
        public void Execute_After_V100220ObsoleteIndex()
        {
            var monsterCollectionAddress = MonsterCollectionState.DeriveAddress(_signer, 0);
            var monsterCollectionState = new MonsterCollectionState(
                monsterCollectionAddress,
                1,
                ActionObsoleteConfig.V100220ObsoleteIndex - MonsterCollectionState.RewardInterval);
            var currency = _state.GetGoldCurrency();
            var context = new ActionContext();
            var states = _state
                .SetLegacyState(monsterCollectionAddress, monsterCollectionState.Serialize())
                .MintAsset(context, monsterCollectionAddress, currency * 100);
            var action = new MigrateMonsterCollection(_avatarAddress);
            states = action.Execute(
                new ActionContext
                {
                    PreviousState = states,
                    Signer = _signer,
                    BlockIndex = ActionObsoleteConfig.V100220ObsoleteIndex + 1,
                    RandomSeed = 0,
                });

            Assert.True(
                states.TryGetAvatarState(
                    _signer,
                    _avatarAddress,
                    out var avatarState));

            Assert.Equal(80, avatarState.inventory.Items.First(item => item.item.Id == 400000).count);
            Assert.Equal(1, avatarState.inventory.Items.First(item => item.item.Id == 500000).count);
        }

        [Theory]
        [ClassData(typeof(ExecuteFixture))]
        public void Execute(int collectionLevel, long claimBlockIndex, long receivedBlockIndex, long stakedAmount, (int ItemId, int Quantity)[] expectedItems)
        {
            var collectionAddress = MonsterCollectionState.DeriveAddress(_signer, 0);
            var monsterCollectionState = new MonsterCollectionState(collectionAddress, collectionLevel, 0);
            var gameConfig = new GameConfigState();
            var currency = _state.GetGoldCurrency();

            var context = new ActionContext();
            var states = _state
                .SetLegacyState(collectionAddress, monsterCollectionState.Serialize())
                .MintAsset(context, monsterCollectionState.address, stakedAmount * currency)
                .SetLegacyState(GameConfigState.Address, gameConfig.Serialize());

            Assert.Equal(0, states.GetAgentState(_signer).MonsterCollectionRound);
            Assert.Equal(0 * currency, states.GetBalance(_signer, currency));
            Assert.Equal(stakedAmount * currency, states.GetBalance(collectionAddress, currency));

            var action = new MigrateMonsterCollection(_avatarAddress);
            states = action.Execute(
                new ActionContext
                {
                    PreviousState = states,
                    Signer = _signer,
                    BlockIndex = claimBlockIndex,
                    RandomSeed = 0,
                });

            Assert.True(states.TryGetLegacyStakeState(_signer, out LegacyStakeState stakeState));
            Assert.Equal(
                0 * currency,
                states.GetBalance(monsterCollectionState.address, currency));
            Assert.Equal(stakedAmount * currency, states.GetStaked(_signer));
            Assert.Equal(receivedBlockIndex, stakeState.ReceivedBlockIndex);
            Assert.Equal(monsterCollectionState.StartedBlockIndex, stakeState.StartedBlockIndex);
            Assert.True(
                states.TryGetAvatarState(
                    _signer,
                    _avatarAddress,
                    out var avatarState));
            foreach (var (itemId, quantity) in expectedItems)
            {
                Assert.True(avatarState.inventory.HasItem(itemId, quantity));
            }
        }

        [Fact]
        public void Serialization()
        {
            var action = new MigrateMonsterCollection(_avatarAddress);
            var deserialized = new MigrateMonsterCollection();
            deserialized.LoadPlainValue(action.PlainValue);
            Assert.Equal(action.PlainValue, deserialized.PlainValue);
        }

        private class ExecuteFixture : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new ()
            {
                new object[]
                {
                    1,
                    MonsterCollectionState.RewardInterval,
                    MonsterCollectionState.RewardInterval,
                    500,
                    new (int, int)[]
                    {
                        (400000, 80),
                        (500000, 1),
                    },
                },
                new object[]
                {
                    2,
                    MonsterCollectionState.RewardInterval,
                    MonsterCollectionState.RewardInterval,
                    2300,
                    new (int, int)[]
                    {
                        (400000, 265),
                        (500000, 2),
                    },
                },
                new object[]
                {
                    3,
                    MonsterCollectionState.RewardInterval,
                    MonsterCollectionState.RewardInterval,
                    9500,
                    new (int, int)[]
                    {
                        (400000, 1265),
                        (500000, 5),
                    },
                },
                new object[]
                {
                    4,
                    MonsterCollectionState.RewardInterval,
                    MonsterCollectionState.RewardInterval,
                    63500,
                    new (int, int)[]
                    {
                        (400000, 8465),
                        (500000, 31),
                    },
                },
                new object[]
                {
                    5,
                    MonsterCollectionState.RewardInterval,
                    MonsterCollectionState.RewardInterval,
                    333500,
                    new (int, int)[]
                    {
                        (400000, 45965),
                        (500000, 161),
                    },
                },
                new object[]
                {
                    6,
                    MonsterCollectionState.RewardInterval,
                    MonsterCollectionState.RewardInterval,
                    813500,
                    new (int, int)[]
                    {
                        (400000, 120965),
                        (500000, 361),
                    },
                },
                new object[]
                {
                    7,
                    MonsterCollectionState.RewardInterval,
                    MonsterCollectionState.RewardInterval,
                    2313500,
                    new (int, int)[]
                    {
                        (400000, 350965),
                        (500000, 1121),
                    },
                },
                new object[]
                {
                    7,
                    MonsterCollectionState.RewardInterval - 1,
                    0, // Because it cannot claim rewards.
                    2313500,
                    new (int, int)[]
                    {
                        (400000, 0),
                        (500000, 0),
                    },
                },
            };

            public IEnumerator<object[]> GetEnumerator()
            {
                return _data.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _data.GetEnumerator();
            }
        }
    }
}
