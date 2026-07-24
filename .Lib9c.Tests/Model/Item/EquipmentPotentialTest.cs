namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Nekoyume.Model.Item;
    using Xunit;

    public class EquipmentPotentialTest
    {
        [Fact]
        public void Empty_IsEmpty()
        {
            var empty = EquipmentPotential.Empty;

            Assert.True(empty.IsEmpty);
            Assert.Equal(0, empty.UnlockedSlotCount);
            Assert.Empty(empty.Slots);
        }

        [Fact]
        public void Serialize_Empty_RoundTrips()
        {
            var empty = EquipmentPotential.Empty;
            var serialized = empty.Serialize();
            var deserialized = new EquipmentPotential(serialized);

            Assert.Equal(empty, deserialized);
            Assert.True(deserialized.IsEmpty);
            Assert.Equal(serialized, deserialized.Serialize());
        }

        [Fact]
        public void Serialize_WithSlots_RoundTrips()
        {
            var potential = new EquipmentPotential(
                3,
                new List<PotentialOptionSlot>
                {
                    new PotentialOptionSlot(700001, 12m),
                    new PotentialOptionSlot(700002, 3m),
                    new PotentialOptionSlot(700003, 1.5m),
                });

            var serialized = potential.Serialize();
            var deserialized = new EquipmentPotential(serialized);

            Assert.Equal(potential, deserialized);
            Assert.Equal(3, deserialized.UnlockedSlotCount);
            Assert.Equal(3, deserialized.Slots.Count);
            Assert.Equal(700002, deserialized.Slots[1].OptionRowId);
            Assert.Equal(3m, deserialized.Slots[1].Value);
            Assert.Equal(1.5m, deserialized.Slots[2].Value);
            Assert.Equal(serialized, deserialized.Serialize());
        }

        [Fact]
        public void Serialize_WritesVersionFirst()
        {
            var serialized = (List)EquipmentPotential.Empty.Serialize();

            Assert.Equal((Integer)EquipmentPotential.SerializationVersion, serialized[0]);
        }

        [Fact]
        public void Deserialize_UnsupportedVersion_Throws()
        {
            var invalid = List.Empty
                .Add(EquipmentPotential.SerializationVersion + 1)
                .Add(0)
                .Add(new List());

            Assert.Throws<ArgumentException>(() => new EquipmentPotential(invalid));
        }

        [Fact]
        public void Deserialize_NonList_Throws()
        {
            Assert.Throws<ArgumentException>(() => new EquipmentPotential((Text)"not-a-list"));
        }

        [Fact]
        public void PotentialOptionSlot_Serialize_RoundTrips()
        {
            var slot = new PotentialOptionSlot(700010, 42m);
            var deserialized = new PotentialOptionSlot(slot.Serialize());

            Assert.Equal(slot, deserialized);
            Assert.Equal(700010, deserialized.OptionRowId);
            Assert.Equal(42m, deserialized.Value);
        }
    }
}
