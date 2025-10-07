using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class ConsumableSlotUnlockException : InvalidOperationException
    {
        public ConsumableSlotUnlockException(string s) : base(s)
        {
        }

        protected ConsumableSlotUnlockException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
