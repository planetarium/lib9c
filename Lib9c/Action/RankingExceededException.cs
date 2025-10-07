using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class RankingExceededException : InvalidOperationException
    {
        public RankingExceededException()
        {
        }

        public RankingExceededException(string message) : base(message)
        {
        }

        protected RankingExceededException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
}
