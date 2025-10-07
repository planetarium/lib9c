using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class NotEnoughRankException : InvalidOperationException
    {
        public NotEnoughRankException()
        {
        }

        public NotEnoughRankException(string message) : base(message)
        {
        }

        public NotEnoughRankException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
}
