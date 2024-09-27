using System;
using System.Runtime.Serialization;

namespace Nekoyume.Module.CombinationSlot
{
    [Serializable]
    public class CombinationSlotNotFoundException : Exception
    {
        public CombinationSlotNotFoundException(string message) : base(message)
        {
        }

        protected CombinationSlotNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
    
    [Serializable]
    public class DuplicatedCombinationSlotIndexException : Exception
    {
        public DuplicatedCombinationSlotIndexException(string message) : base(message)
        {
        }

        protected DuplicatedCombinationSlotIndexException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
}
