namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Xunit;
    using Xunit.Abstractions;

    public class RaidSimulatorV3Test
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;

        public RaidSimulatorV3Test(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _random = new TestRandom();

            _avatarState = AvatarState.Create(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            _avatarState.level = 250;
        }

        [Fact]
        public void Simulate()
        {
            var bossId = _tableSheets.WorldBossListSheet.First().Value.BossId;
            var simulator = new RaidSimulator(
                bossId,
                _random,
                _avatarState,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(BattleType.Raid),
                _tableSheets.GetRaidSimulatorSheets(),
                _tableSheets.CostumeStatSheet,
                new List<StatModifier>
                {
                    new (StatType.DEF, StatModifier.OperationType.Percentage, 100),
                },
                _tableSheets.BuffLimitSheet,
                _tableSheets.BuffLinkSheet);
            Assert.Equal(_random, simulator.Random);
            Assert.Equal(simulator.Player.Stats.BaseStats.DEF * 2, simulator.Player.Stats.DEF);
            Assert.Equal(simulator.Player.Stats.BaseStats.DEF, simulator.Player.Stats.CollectionStats.DEF);

            var log = simulator.Simulate();

            var turn = log.OfType<WaveTurnEnd>().Count();
            Assert.Equal(simulator.TurnNumber, turn);

            var expectedWaveCount = _tableSheets.WorldBossCharacterSheet[bossId].WaveStats.Count;
            Assert.Equal(expectedWaveCount, log.waveCount);

            var deadEvents = log.OfType<Dead>();
            foreach (var dead in deadEvents)
            {
                Assert.True(dead.Character.IsDead);
            }
        }

        [Fact]
        public void TestSpeedMultiplierBySkill()
        {
            var bossId = _tableSheets.WorldBossListSheet.First().Value.BossId;

            // Unskilled
            var equipmentRow =
                _tableSheets.EquipmentItemSheet.OrderedList.First(e => e.Id == 10114000);
            var item = (Equipment)ItemFactory.CreateItem(equipmentRow, new TestRandom());
            Assert.Empty(item.Skills);
            item.equipped = true;
            _avatarState.inventory.AddItem(item);

            var simulator = new RaidSimulator(
                bossId,
                new TestRandom(),
                _avatarState,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(BattleType.Raid),
                _tableSheets.GetRaidSimulatorSheets(),
                _tableSheets.CostumeStatSheet,
                new List<StatModifier>(),
                _tableSheets.BuffLimitSheet,
                _tableSheets.BuffLinkSheet
            );
            var player = simulator.Player;
            var unskilledLogs = simulator.Simulate();
            var unSkilledActions = unskilledLogs.Where(l => l.Character?.Id == player.Id);
            foreach (var log in unSkilledActions)
            {
                _testOutputHelper.WriteLine($"{log}");
            }

            _testOutputHelper.WriteLine("==========");

            // Reset
            _avatarState.inventory.Equipments.First().equipped = false;
            _avatarState.inventory.RemoveNonFungibleItem(item.ItemId);
            Assert.Empty(_avatarState.inventory.Equipments);

            // Skilled
            var skilledItem = (Equipment)ItemFactory.CreateItem(equipmentRow, new TestRandom());
            Assert.Empty(skilledItem.Skills);
            CombinationEquipment.AddSkillOption(
                new AgentState(new PrivateKey().Address),
                skilledItem,
                new TestRandom(0),
                _tableSheets.EquipmentItemSubRecipeSheetV2.OrderedList!.First(r => r.Id == 10),
                _tableSheets.EquipmentItemOptionSheet,
                _tableSheets.SkillSheet
            );
            Assert.True(skilledItem.Skills.Any());
            skilledItem.equipped = true;
            _avatarState.inventory.AddItem(skilledItem);

            simulator = new RaidSimulator(
                bossId,
                new TestRandom(),
                _avatarState,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(BattleType.Raid),
                _tableSheets.GetRaidSimulatorSheets(),
                _tableSheets.CostumeStatSheet,
                new List<StatModifier>(),
                _tableSheets.BuffLimitSheet,
                _tableSheets.BuffLinkSheet
            );
            player = simulator.Player;
            var skilledLogs = simulator.Simulate();
            var skilledActions = skilledLogs.Where(l => l.Character?.Id == player.Id);
            foreach (var log in skilledActions)
            {
                _testOutputHelper.WriteLine($"{log}");
            }

            Assert.True(skilledActions.Count() > unSkilledActions.Count());
        }
    }
}
