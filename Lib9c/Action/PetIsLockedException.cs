using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class PetIsLockedException : Exception
    {
        public PetIsLockedException(string msg) : base(msg)
        {
        }

        public PetIsLockedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
