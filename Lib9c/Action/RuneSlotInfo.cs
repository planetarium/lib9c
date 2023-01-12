using System;
using Bencodex.Types;
using Lib9c.Model.State;

namespace Lib9c.Action
{
    [Serializable]
    public class RuneSlotInfo
    {
        public int SlotIndex { get; }
        public int RuneId { get; }

        public RuneSlotInfo(int slotIndex, int runeId)
        {
            SlotIndex = slotIndex;
            RuneId = runeId;
        }

        public RuneSlotInfo(List serialized)
        {
            SlotIndex = serialized[0].ToInteger();
            RuneId = serialized[1].ToInteger();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(SlotIndex.Serialize())
                .Add(RuneId.Serialize());
        }
    }
}
