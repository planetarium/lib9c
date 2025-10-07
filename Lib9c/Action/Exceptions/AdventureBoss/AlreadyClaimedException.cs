using System;
using System.Runtime.Serialization;

namespace Lib9c.Action.Exceptions.AdventureBoss
{
    [Serializable]
    public class AlreadyClaimedException : Exception
    {
        public AlreadyClaimedException()
        {
        }

        protected AlreadyClaimedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public AlreadyClaimedException(string msg) : base(msg)
        {
        }
    }
}
