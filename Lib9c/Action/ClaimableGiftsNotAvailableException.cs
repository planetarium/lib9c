using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class ClaimableGiftsNotAvailableException : Exception
    {
        public ClaimableGiftsNotAvailableException()
        {
        }

        public ClaimableGiftsNotAvailableException(string msg) : base(msg)
        {
        }

        public ClaimableGiftsNotAvailableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
