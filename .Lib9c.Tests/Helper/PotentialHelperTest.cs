namespace Lib9c.Tests.Helper
{
    using System;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class PotentialHelperTest
    {
        private readonly TableSheets _tableSheets;

        public PotentialHelperTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Roll_FillsSlotsWithinRange()
        {
            var pool = _tableSheets.EquipmentPotentialOptionPoolSheet;
            var equipment = CreateWeapon();

            var potential = PotentialHelper.Roll(equipment, 3, pool, new TestRandom(1));

            Assert.Equal(3, potential.UnlockedSlotCount);
            Assert.Equal(3, potential.Slots.Count);
            foreach (var slot in potential.Slots)
            {
                var row = pool[slot.OptionRowId];
                Assert.Equal(ItemSubType.Weapon, row.ItemSubType);
                Assert.InRange(slot.Value, row.ValueMin, row.ValueMax);
            }
        }

        [Fact]
        public void Roll_IsDeterministicForSameSeed()
        {
            var pool = _tableSheets.EquipmentPotentialOptionPoolSheet;
            var equipment = CreateWeapon();

            var a = PotentialHelper.Roll(equipment, 3, pool, new TestRandom(7));
            var b = PotentialHelper.Roll(equipment, 3, pool, new TestRandom(7));

            Assert.Equal(a, b);
        }

        [Fact]
        public void Roll_GoldenOutput_LocksAlgorithm()
        {
            // Golden test: pins the exact roll output for a fixed seed. Because rolled results are
            // recorded in block history, any change to the selection / value-roll algorithm would
            // break consensus on replay; this test forces such a change to be an explicit edit here.
            var pool = _tableSheets.EquipmentPotentialOptionPoolSheet;
            var equipment = CreateWeapon();

            var potential = PotentialHelper.Roll(equipment, 3, pool, new TestRandom(2026));

            var actual = potential.Slots
                .Select(s => (s.OptionRowId, s.Value))
                .ToArray();
            var expected = new[]
            {
                (700001, 1L),
                (700003, 12L),
                (700003, 9L),
            };
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Roll_ZeroSlots_ReturnsEmpty()
        {
            var pool = _tableSheets.EquipmentPotentialOptionPoolSheet;
            var equipment = CreateWeapon();

            var potential = PotentialHelper.Roll(equipment, 0, pool, new TestRandom(1));

            Assert.Equal(0, potential.UnlockedSlotCount);
            Assert.Empty(potential.Slots);
        }

        private Equipment CreateWeapon()
        {
            var row = _tableSheets.EquipmentItemSheet.OrderedList
                .First(r => r.ItemSubType == ItemSubType.Weapon);
            return (Equipment)ItemFactory.CreateItemUsable(row, Guid.NewGuid(), 0);
        }
    }
}
