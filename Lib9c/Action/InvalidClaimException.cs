using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class InvalidClaimException : InvalidOperationException
    {
        public InvalidClaimException()
        {
        }

        public InvalidClaimException(string msg) : base(msg)
        {
        }

        protected InvalidClaimException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
