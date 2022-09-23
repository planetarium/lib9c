using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
{
    [Serializable]
    public class RankingExceededException: InvalidOperationException
    {
        public RankingExceededException()
        {
        }

        protected RankingExceededException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
