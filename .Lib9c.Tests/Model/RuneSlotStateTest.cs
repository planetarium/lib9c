using Bencodex.Types;
using Lib9c.Model.EnumType;
using Lib9c.Model.State;
using Xunit;

namespace Lib9c.Tests.Model
{
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
    }
}
