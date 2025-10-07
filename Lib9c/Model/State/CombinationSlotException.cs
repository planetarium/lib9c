using System;
using System.Runtime.Serialization;

namespace Lib9c.Model.State
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
