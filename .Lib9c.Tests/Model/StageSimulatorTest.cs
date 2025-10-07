namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Action;
    using Lib9c.Battle;
    using Lib9c.Model.BattleStatus;
    using Lib9c.Model.EnumType;
    using Lib9c.Model.Item;
    using Lib9c.Model.Quest;
    using Lib9c.Model.Stat;
    using Lib9c.Model.State;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Libplanet.Crypto;
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

            _avatarState = AvatarState.Create(
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
            var row = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume = (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[row.CostumeId], _random);
            costume.equipped = true;
            _avatarState.inventory.AddItem(costume);

            var simulator = new StageSimulator(
                _random,
                _avatarState,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(BattleType.Adventure),
                new List<Lib9c.Model.Skill.Skill>(),
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
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>
                {
                    new (StatType.ATK, StatModifier.OperationType.Add, 100),
                },
                _tableSheets.BuffLimitSheet,
                _tableSheets.BuffLinkSheet
            );

            var player = simulator.Player;
            Assert.Equal(row.Stat, player.Stats.CostumeStats.ATK);
            Assert.Equal(100, player.Stats.CollectionStats.ATK);
            Assert.Equal(100 + row.Stat + player.Stats.BaseStats.ATK, player.Stats.ATK);
            while (player.Level == 1)
            {
                simulator.Simulate();
            }

            var player2 = simulator.Player;
            Assert.Equal(row.Stat, player2.Stats.CostumeStats.ATK);
            Assert.Equal(2, player2.Level);
            Assert.Equal(1, player2.eventMap[(int)QuestEventType.Level]);
            Assert.True(simulator.Log.OfType<GetExp>().Any());

            var filtered =
                simulator.Log
                    .Select(e => e.GetType())
                    .Where(
                        type =>
                            type != typeof(GetReward) ||
                            type != typeof(DropBox));
            Assert.Equal(typeof(WaveTurnEnd), filtered.Last());
            Assert.Equal(1, simulator.Log.OfType<WaveTurnEnd>().First().TurnNumber);
        }

        [Fact]
        public void TestSpeedModifierBySkill()
        {
            var equipmentRow =
                _tableSheets.EquipmentItemSheet.OrderedList.First(e => e.Id == 10114000);
            var item = (Equipment)ItemFactory.CreateItem(equipmentRow, new TestRandom());
            Assert.Empty(item.Skills);
            item.equipped = true;
            _avatarState.inventory.AddItem(item);

            // Simulate with un-skilled equipment
            var simulator = new StageSimulator(
                new TestRandom(1),
                _avatarState,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(BattleType.Adventure),
                new List<Lib9c.Model.Skill.Skill>(),
                1,
                3,
                _tableSheets.StageSheet[3],
                _tableSheets.StageWaveSheet[7],
                false,
                20,
                _tableSheets.GetSimulatorSheets(),
                _tableSheets.EnemySkillSheet,
                _tableSheets.CostumeStatSheet,
                StageSimulator.GetWaveRewards(
                    new TestRandom(1),
                    _tableSheets.StageSheet[3],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                _tableSheets.BuffLimitSheet,
                _tableSheets.BuffLinkSheet
            );
            var unskilledPlayer = simulator.Player;
            Assert.Contains(item, unskilledPlayer.Inventory.Equipments);
            simulator.Simulate();

            var unSkilledActions = simulator.Log.Where(l => l.Character?.Id == unskilledPlayer.Id);
            /* foreach (var log in unSkilledActions)
             * {
             *     _testOutputHelper.WriteLine($"{log}");
             * }
             * _testOutputHelper.WriteLine("=============================================");
             */

            // Reset and simulate with skilled equipment
            _avatarState.inventory.Equipments.First().equipped = false;
            _avatarState.inventory.RemoveNonFungibleItem(item.ItemId);
            Assert.Empty(_avatarState.inventory.Equipments);
            var skilledItem = (Equipment)ItemFactory.CreateItem(equipmentRow, new TestRandom());
            Assert.Empty(skilledItem.Skills);

            // Add BlowAttack
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
            simulator = new StageSimulator(
                new TestRandom(1),
                _avatarState,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(BattleType.Adventure),
                new List<Lib9c.Model.Skill.Skill>(),
                1,
                3,
                _tableSheets.StageSheet[3],
                _tableSheets.StageWaveSheet[7],
                false,
                20,
                _tableSheets.GetSimulatorSheets(),
                _tableSheets.EnemySkillSheet,
                _tableSheets.CostumeStatSheet,
                StageSimulator.GetWaveRewards(
                    new TestRandom(1),
                    _tableSheets.StageSheet[3],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                _tableSheets.BuffLimitSheet,
                _tableSheets.BuffLinkSheet
            );
            var skilledPlayer = simulator.Player;
            Assert.Contains(skilledItem, skilledPlayer.Inventory.Equipments);
            simulator.Simulate();
            var skilledActions = simulator.Log.Where(l => l.Character?.Id == skilledPlayer.Id);
            /*
             * foreach (var log in skilledActions)
             * {
             *     _testOutputHelper.WriteLine($"{log}");
             * }
             */

            Assert.Contains(skilledActions, e => e is BlowAttack);
            // Skill scales speed by 0.9, so this makes way more player actions.
            Assert.True(skilledActions.Count() > unSkilledActions.Count());
        }
    }
}
