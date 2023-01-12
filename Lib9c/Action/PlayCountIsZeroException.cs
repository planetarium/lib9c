using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class PlayCountIsZeroException : Exception
    {
        public PlayCountIsZeroException(string msg) : base(msg)
        {
        }

        public PlayCountIsZeroException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
