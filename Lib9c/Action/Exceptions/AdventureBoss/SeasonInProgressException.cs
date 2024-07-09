using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    [Serializable]
    public class SeasonInProgressException : Exception
    {
        public SeasonInProgressException()
        {
        }

        protected SeasonInProgressException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public SeasonInProgressException(string msg) : base(msg)
        {
        }
    }
}
