using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class NotEnoughActionPointException : Exception
    {
        public NotEnoughActionPointException(string msg) : base(msg)
        {
        }

        public NotEnoughActionPointException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
