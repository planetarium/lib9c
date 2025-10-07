using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class MonsterCollectionExistingClaimableException : Exception
    {
        public MonsterCollectionExistingClaimableException()
        {
        }

        public MonsterCollectionExistingClaimableException(string message) : base(message)
        {
        }

        public MonsterCollectionExistingClaimableException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MonsterCollectionExistingClaimableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
