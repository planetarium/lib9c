using System;
using System.Runtime.Serialization;

namespace Lib9c.Action.Exceptions.AdventureBoss
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
