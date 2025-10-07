using System;
using System.Runtime.Serialization;

namespace Lib9c.Action.Exceptions.AdventureBoss
{
    [Serializable]
    public class InsufficientStakingException : Exception
    {
        public InsufficientStakingException()
        {
        }

        protected InsufficientStakingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public InsufficientStakingException(string msg) : base(msg)
        {
        }
    }
}
