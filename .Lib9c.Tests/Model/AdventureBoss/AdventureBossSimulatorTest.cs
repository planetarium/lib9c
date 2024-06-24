namespace Lib9c.Tests.Model.AdventureBoss
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume.Battle.AdventureBoss;
    using Nekoyume.Data;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.BattleStatus.AdventureBoss;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Xunit;
    using Xunit.Abstractions;

    public class AdventureBossSimulatorTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;

        public AdventureBossSimulatorTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _random = new TestRandom();
            _avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
        }

        [Fact]
        public AdventureBossSimulator Simulate()
        {
            var adventureBossData = _tableSheets.AdventureBossSheet.Values.First();
            var row = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume =
                (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[row.CostumeId], _random);
            costume.equipped = true;
            _avatarState.inventory.AddItem(costume);

            var simulator = new AdventureBossSimulator(
                adventureBossData.BossId,
                1,
                _random,
                _avatarState,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(BattleType.Adventure),
                _tableSheets.AdventureBossFloorSheet[1],
                _tableSheets.AdventureBossFloorWaveSheet[1],
                _tableSheets.GetSimulatorSheets(),
                _tableSheets.EnemySkillSheet,
                _tableSheets.CostumeStatSheet,
                AdventureBossSimulator.GetWaveRewards(
                    _random,
                    _tableSheets.AdventureBossFloorSheet[1],
                    _tableSheets.MaterialItemSheet
                ),
                new List<StatModifier>
                {
                    new (StatType.ATK, StatModifier.OperationType.Add, 100),
                },
                _tableSheets.DeBuffLimitSheet
            );

            var player = simulator.Player;
            Assert.Equal(row.Stat, player.Stats.CostumeStats.ATK);
            Assert.Equal(100, player.Stats.CollectionStats.ATK);
            Assert.Equal(100 + row.Stat + player.Stats.BaseStats.ATK, player.Stats.ATK);

            simulator.Simulate();

            Assert.True(simulator.Log.OfType<DropBox>().Any());
            var filtered =
                simulator.Log
                    .Select(e => e.GetType())
                    .Where(type => type != typeof(GetReward) && type != typeof(DropBox));
            Assert.Equal(typeof(WaveTurnEnd), filtered.Last());
            Assert.Equal(1, simulator.Log.OfType<WaveTurnEnd>().First().TurnNumber);
            Assert.NotEmpty(simulator.Log.OfType<StageBuff>());

            return simulator;
        }

        [Theory]
        [InlineData(true, 1, 1)]
        [InlineData(true, 1, 5)]
        [InlineData(true, 1, 3)]
        [InlineData(false, 1, 1)]
        [InlineData(false, 1, 5)]
        [InlineData(false, 1, 3)]
        [InlineData(false, 21, 23)] // For Boss ID 2
        public void AddBreakthrough(bool simulate, int firstFloorId, int lastFloorId)
        {
            AdventureBossSimulator simulator;
            if (simulate)
            {
                simulator = Simulate();
            }
            else
            {
                var adventureBossData = _tableSheets.AdventureBossSheet.Values.First();
                var row = _tableSheets.CostumeStatSheet.Values.First(
                    r => r.StatType == StatType.ATK);
                var costume =
                    (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[row.CostumeId], _random);
                costume.equipped = true;
                _avatarState.inventory.AddItem(costume);

                simulator = new AdventureBossSimulator(
                    adventureBossData.BossId,
                    1,
                    _random,
                    _avatarState,
                    new List<Guid>(),
                    new AllRuneState(),
                    new RuneSlotState(BattleType.Adventure),
                    _tableSheets.AdventureBossFloorSheet[lastFloorId],
                    _tableSheets.AdventureBossFloorWaveSheet[lastFloorId],
                    _tableSheets.GetSimulatorSheets(),
                    _tableSheets.EnemySkillSheet,
                    _tableSheets.CostumeStatSheet,
                    AdventureBossSimulator.GetWaveRewards(
                        _random,
                        _tableSheets.AdventureBossFloorSheet[1],
                        _tableSheets.MaterialItemSheet
                    ),
                    new List<StatModifier>
                    {
                        new (StatType.ATK, StatModifier.OperationType.Add, 100),
                    },
                    _tableSheets.DeBuffLimitSheet
                );
            }

            simulator.AddBreakthrough(firstFloorId, lastFloorId, _tableSheets.AdventureBossFloorWaveSheet);

            Assert.Equal(typeof(SpawnPlayer), simulator.Log.events.First().GetType());
            if (!simulate)
            {
                // +2: 1 for last floor, 1 for SpawnPlayer
                Assert.Equal(lastFloorId - firstFloorId + 2, simulator.Log.events.Count);
            }

            var filtered = simulator.Log.events.OfType<Breakthrough>();
            Assert.Equal(lastFloorId - firstFloorId + 1, filtered.Count());

            if (simulate)
            {
                var anotherActions = simulator.Log.events.Where(e =>
                    e.GetType() != typeof(Breakthrough) && e.GetType() != typeof(SpawnPlayer));
                Assert.NotEmpty(anotherActions);
                Assert.NotEmpty(simulator.Log.OfType<StageBuff>());
            }
        }
    }
}
