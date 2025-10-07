using System;
using System.Runtime.Serialization;

namespace Lib9c.Action.Exceptions.AdventureBoss
{
    [Serializable]
    public class ClaimExpiredException : Exception
    {
        public ClaimExpiredException()
        {
        }

        protected ClaimExpiredException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public ClaimExpiredException(string msg) : base(msg)
        {
        }
    }
}
