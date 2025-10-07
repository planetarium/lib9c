using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class CombinationSlotUnlockException : InvalidOperationException
    {
        public CombinationSlotUnlockException(string s) : base(s)
        {
        }

        protected CombinationSlotUnlockException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class InvalidSlotIndexException : InvalidOperationException
    {
        public InvalidSlotIndexException(string s) : base(s)
        {
        }

        protected InvalidSlotIndexException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class SlotAlreadyUnlockedException : InvalidOperationException
    {
        public SlotAlreadyUnlockedException(string s) : base(s)
        {
        }

        protected SlotAlreadyUnlockedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
