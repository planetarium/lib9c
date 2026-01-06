namespace Lib9c.Tests.Model
{
    using System.Linq;
    using Bencodex.Types;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.State;
    using Xunit;

    public class RuneSlotStateTest
    {
        [Fact]
        public void Serialize()
        {
            var state = new RuneSlotState(BattleType.Adventure);
            var serialized = (List)state.Serialize();
            var deserialized = new RuneSlotState(serialized);

            Assert.Equal(state.Serialize(), deserialized.Serialize());
        }

        [Fact]
        public void Deserialize_Add_Slots()
        {
            var runeSlotState = new RuneSlotState(BattleType.Adventure);
            var serialized = (List)runeSlotState.Serialize();
            var rawSlots = new List(((List)serialized[1]).Take(6));
            serialized = List.Empty.Add(BattleType.Adventure.Serialize()).Add(rawSlots);
            var deserialized = new RuneSlotState(serialized);

            var runeSlots = deserialized.GetRuneSlot();
            Assert.Equal(10, runeSlots.Count);
            Assert.Equal(2, runeSlots.Count(r => r.RuneSlotType == RuneSlotType.Crystal));
            Assert.Equal(4, runeSlots.Count(r => r.RuneSlotType == RuneSlotType.Default));
        }

        [Fact]
        public void Deserialize_Add_Default_Slots()
        {
            var runeSlotState = new RuneSlotState(BattleType.Adventure);
            var serialized = (List)runeSlotState.Serialize();
            var rawSlots = new List(((List)serialized[1]).Take(8));
            serialized = List.Empty.Add(BattleType.Adventure.Serialize()).Add(rawSlots);
            var deserialized = new RuneSlotState(serialized);

            var runeSlots = deserialized.GetRuneSlot();
            Assert.Equal(10, runeSlots.Count);
            Assert.Equal(4, runeSlots.Count(r => r.RuneSlotType == RuneSlotType.Default));
            Assert.Equal(2, runeSlots.Count(r => r.RuneSlotType == RuneSlotType.Crystal));
        }
    }
}
