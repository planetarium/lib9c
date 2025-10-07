using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class EquipmentSlotUnlockException : Exception
    {
        public EquipmentSlotUnlockException()
        {
        }

        public EquipmentSlotUnlockException(string msg) : base(msg)
        {
        }

        protected EquipmentSlotUnlockException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

    }
}
