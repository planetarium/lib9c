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
        public void Simulate()
        {
            var adventureBossData = AdventureBossGameData.AdventureBossRewards.First();
            var row = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume =
                (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[row.CostumeId], _random);
            costume.equipped = true;
            _avatarState.inventory.AddItem(costume);

            var simulator = new AdventureBossSimulator(
                adventureBossData.BossId,
                adventureBossData.exploreReward.Keys.First(), // 1
                _random,
                _avatarState,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(BattleType.Adventure),
                _tableSheets.FloorSheet[1],
                _tableSheets.FloorWaveSheet[1],
                _tableSheets.GetSimulatorSheets(),
                _tableSheets.EnemySkillSheet,
                _tableSheets.CostumeStatSheet,
                AdventureBossSimulator.GetWaveRewards(
                    _random,
                    _tableSheets.FloorSheet[1],
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
        }
    }
}
