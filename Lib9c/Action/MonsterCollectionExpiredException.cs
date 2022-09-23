using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
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
