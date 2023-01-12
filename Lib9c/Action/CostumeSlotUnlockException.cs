using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class CostumeSlotUnlockException : InvalidOperationException
    {
        public CostumeSlotUnlockException(string s) : base(s)
        {
        }

        protected CostumeSlotUnlockException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
