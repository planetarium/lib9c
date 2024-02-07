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
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Quest;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Xunit;
    using Xunit.Abstractions;

    public class StageSimulatorTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;

        public StageSimulatorTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _random = new TestRandom();

            _avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
        }

        [Fact]
        public void Simulate()
        {
            var row = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume = (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[row.CostumeId], _random);
            costume.equipped = true;
            _avatarState.inventory.AddItem(costume);

            var simulator = new StageSimulator(
                _random,
                _avatarState,
                new List<Guid>(),
                null,
                new List<Nekoyume.Model.Skill.Skill>(),
                1,
                1,
                _tableSheets.StageSheet[1],
                _tableSheets.StageWaveSheet[1],
                false,
                20,
                _tableSheets.GetSimulatorSheets(),
                _tableSheets.EnemySkillSheet,
                _tableSheets.CostumeStatSheet,
                StageSimulator.GetWaveRewards(
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet)
            );

            var player = simulator.Player;
            Assert.Equal(row.Stat, player.Stats.OptionalStats.ATK);
            while (player.Level == 1)
            {
                simulator.Simulate();
            }

            var player2 = simulator.Player;
            Assert.Equal(row.Stat, player2.Stats.OptionalStats.ATK);
            Assert.Equal(2, player2.Level);
            Assert.Equal(1, player2.eventMap[(int)QuestEventType.Level]);
            Assert.True(simulator.Log.OfType<GetExp>().Any());

            var filtered =
                simulator.Log
                    .Select(e => e.GetType())
                    .Where(type =>
                        type != typeof(GetReward) ||
                        type != typeof(DropBox));
            Assert.Equal(typeof(WaveTurnEnd), filtered.Last());
            Assert.Equal(1, simulator.Log.OfType<WaveTurnEnd>().First().TurnNumber);
        }

        [Fact]
        public void TestSkillSpeed()
        {
            var equipmentRow =
                _tableSheets.EquipmentItemSheet.OrderedList.First(e => e.Id == 10114000);
            var item = (Equipment)ItemFactory.CreateItem(equipmentRow, new TestRandom());
            Assert.Empty(item.Skills);
            item.equipped = true;
            _avatarState.inventory.AddItem(item);

            // Simulate with un-skilled equipment
            var simulator = new StageSimulator(
                _random,
                _avatarState,
                new List<Guid>(),
                null,
                new List<Nekoyume.Model.Skill.Skill>(),
                1,
                1,
                _tableSheets.StageSheet[1],
                _tableSheets.StageWaveSheet[1],
                false,
                20,
                _tableSheets.GetSimulatorSheets(),
                _tableSheets.EnemySkillSheet,
                _tableSheets.CostumeStatSheet,
                StageSimulator.GetWaveRewards(
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet)
            );
            var unskilledPlayer = simulator.Player;
            Assert.Contains(item, unskilledPlayer.Inventory.Equipments);
            simulator.Simulate();
            var unSkilledLogs = simulator.Log.Count(l => l.Character == unskilledPlayer);

            // Reset and simulate with skilled equipment
            _avatarState.inventory.Equipments.First().equipped = false;
            _avatarState.inventory.RemoveNonFungibleItem(item.ItemId);
            Assert.Empty(_avatarState.inventory.Equipments);
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
            var random = new TestRandom(1);
            simulator = new StageSimulator(
                random,
                _avatarState,
                new List<Guid>(),
                null,
                new List<Nekoyume.Model.Skill.Skill>(),
                1,
                1,
                _tableSheets.StageSheet[1],
                _tableSheets.StageWaveSheet[1],
                false,
                20,
                _tableSheets.GetSimulatorSheets(),
                _tableSheets.EnemySkillSheet,
                _tableSheets.CostumeStatSheet,
                StageSimulator.GetWaveRewards(
                    random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet)
            );
            var skilledPlayer = simulator.Player;
            Assert.Contains(skilledItem, skilledPlayer.Inventory.Equipments);
            simulator.Simulate();
            var skilledActions = simulator.Log.Where(l => l.Character == skilledPlayer);
            foreach (var skill in skilledActions)
            {
                _testOutputHelper.WriteLine(skill.ToString());
            }

            var skilledLogs = simulator.Log.Count(l => l.Character == skilledPlayer);

            Assert.True(skilledLogs > unSkilledLogs);
        }
    }
}
