using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class InvalidWorldException: Exception
    {
        public InvalidWorldException()
        {
        }

        public InvalidWorldException(string msg) : base(msg)
        {
        }

        protected InvalidWorldException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

    }
}
