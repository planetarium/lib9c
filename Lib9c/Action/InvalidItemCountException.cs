using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class InvalidItemCountException : ArgumentOutOfRangeException
    {
        public InvalidItemCountException()
        {
        }

        public InvalidItemCountException(string msg) : base(msg)
        {
        }

        protected InvalidItemCountException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
