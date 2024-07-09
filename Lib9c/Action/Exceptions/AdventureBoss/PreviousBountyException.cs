using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    [Serializable]
    public class PreviousBountyException : Exception
    {
        public PreviousBountyException(string msg) : base(msg)
        {
        }

        public PreviousBountyException()
        {
        }

        protected PreviousBountyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
