using System;
using System.Runtime.Serialization;

namespace Lib9c.Action.Exceptions
{
    [Serializable]
    public class EmptyRewardException : Exception
    {
        public EmptyRewardException()
        {
        }

        public EmptyRewardException(string msg) : base(msg)
        {
        }

        protected EmptyRewardException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
