using System;
using System.Runtime.Serialization;

namespace Lib9c.Action.Exceptions
{
    [Serializable]
    public class DuplicatedCraftSlotIndexException : Exception
    {
        public DuplicatedCraftSlotIndexException(string message) : base(message)
        {
        }

        protected DuplicatedCraftSlotIndexException(
            SerializationInfo info, StreamingContext context
        ) : base(info, context)
        {
        }
    }
}
