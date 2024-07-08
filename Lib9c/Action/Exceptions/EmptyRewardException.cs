using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action.Exceptions
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
