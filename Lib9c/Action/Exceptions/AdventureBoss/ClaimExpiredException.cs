using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action.Exceptions.AdventureBoss
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
