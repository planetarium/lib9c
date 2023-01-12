using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class InvalidMonsterCollectionRoundException : InvalidOperationException
    {
        public InvalidMonsterCollectionRoundException(string msg) : base(msg)
        {
        }

        protected InvalidMonsterCollectionRoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
