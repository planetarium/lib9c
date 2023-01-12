using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class MonsterCollectionExpiredException : InvalidOperationException
    {
        public MonsterCollectionExpiredException(string msg) : base(msg)
        {
        }

        protected MonsterCollectionExpiredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
