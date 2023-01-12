using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class InvalidTradableIdException : InvalidOperationException
    {
        protected InvalidTradableIdException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public InvalidTradableIdException(string message) : base(message)
        {
        }
    }
}
