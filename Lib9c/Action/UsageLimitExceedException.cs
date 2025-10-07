using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class UsageLimitExceedException : InvalidOperationException
    {
        public UsageLimitExceedException()
        {
        }

        public UsageLimitExceedException(string message) : base(message)
        {
        }

        protected UsageLimitExceedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
