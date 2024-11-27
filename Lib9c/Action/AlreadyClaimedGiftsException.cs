using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class AlreadyClaimedGiftsException : Exception
    {
        public AlreadyClaimedGiftsException()
        {
        }

        public AlreadyClaimedGiftsException(string msg) : base(msg)
        {
        }

        public AlreadyClaimedGiftsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
