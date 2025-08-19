namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume;
    using Nekoyume.Battle;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.TableData;
    using Xunit;

    public class CPHelperTest
    {
        private readonly TableSheets _tableSheets;

        public CPHelperTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void GetStatsCp(int level)
        {
            var row =
                _tableSheets.CharacterSheet[GameConfig.DefaultAvatarCharacterId];
            var characterStats = new CharacterStats(row, level);
            Assert.Equal(CPHelper.GetStatsCP(row.ToStats(level), level), CPHelper.GetStatsCP(characterStats, level));
        }

        [Fact]
        public void TotalCP()
        {
            var row =
                _tableSheets.CharacterSheet[GameConfig.DefaultAvatarCharacterId];
            var costumeStatSheet = _tableSheets.CostumeStatSheet;
            var random = new TestRandom();
            // Arrange
            var equipments = new List<Equipment>(); // Populate your equipments
            var equipment = (Equipment)ItemFactory.CreateItem(_tableSheets.EquipmentItemSheet.Values.First(), random);
            var skill = SkillFactory.Get(_tableSheets.SkillSheet.Values.First(), 0, 0, 0, StatType.HP);
            equipment.Skills.Add(skill);
            equipments.Add(equipment);
            var costumes = new List<Costume>(); // Populate your costumes
            var costume = ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet.Values.First(), random.GenerateRandomGuid());
            costumes.Add(costume);
            var runeOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>(); // Populate your runeOptions
            var runeOptionInfo = _tableSheets.RuneOptionSheet.Values.First().LevelOptionMap[1];
            runeOptions.Add(runeOptionInfo);
            var level = 1; // A level for testing
            var collectionStatModifiers = new List<StatModifier>(); // Populate your collectionStatModifiers
            var result = CPHelper.TotalCP(equipments, costumes, runeOptions, level, row, costumeStatSheet, collectionStatModifiers, 0);
            var characterStats = new CharacterStats(row, level);
            characterStats.SetEquipments(equipments, new EquipmentItemSetEffectSheet());
            characterStats.SetCostumeStat(costumes, costumeStatSheet);
            characterStats.AddRuneStat(runeOptionInfo, 0);
            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                if (type == StatType.NONE)
                {
                    continue;
                }

                collectionStatModifiers.Add(new StatModifier(type, StatModifier.OperationType.Percentage, 100));
            }

            characterStats.SetCollections(collectionStatModifiers);
            var ccp = 0m;
            foreach (var (statType, value) in characterStats.CollectionStats.GetStats())
            {
                ccp += CPHelper.GetStatCP(statType, value);
            }

            var collectionCp = CPHelper.DecimalToLong(ccp);
            Assert.Equal(result + collectionCp, CPHelper.TotalCP(equipments, costumes, runeOptions, level, row, costumeStatSheet, collectionStatModifiers, 0));
        }
    }
}
