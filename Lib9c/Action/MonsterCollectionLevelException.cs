using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class MonsterCollectionLevelException : InvalidOperationException
    {
        public MonsterCollectionLevelException()
        {
        }

        public MonsterCollectionLevelException(string msg)
            : base(msg)
        {
        }

        protected MonsterCollectionLevelException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
