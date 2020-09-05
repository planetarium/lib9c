namespace Lib9c.Tests.Model
{
    using System.Linq;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stat;
    using Xunit;

    public class CharacterStatsTest
    {
        private readonly TableSheets _tableSheets;

        public CharacterStatsTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Serialize()
        {
            var row = _tableSheets.CharacterSheet.Values.First();
            var characterStats = new CharacterStats(row, 1);

            var equipments = _tableSheets.EquipmentItemSheet.Values
                .Where(r => r.SetId == 1)
                .Take(2)
                .Select(ItemFactory.CreateItem)
                .Cast<Equipment>()
                .ToList();
            var consumableRow = _tableSheets.ConsumableItemSheet.Values.First();
            var consumable = (Consumable)ItemFactory.CreateItem(consumableRow);
            characterStats.SetAll(1, equipments, new[] { consumable }, _tableSheets.EquipmentItemSetEffectSheet);

            var buffRow = _tableSheets.BuffSheet.Values.First(r => r.StatModifier.StatType == StatType.ATK);
            var buff = new AttackBuff(buffRow);
            characterStats.AddBuff(buff);

            var serialized = characterStats.Serialize();
            Assert.Equal(characterStats, new CharacterStats(serialized));
        }
    }
}
