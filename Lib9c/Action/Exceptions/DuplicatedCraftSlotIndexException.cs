using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action.Exceptions
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
