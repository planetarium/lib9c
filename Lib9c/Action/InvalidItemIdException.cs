using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidItemIdException : ArgumentOutOfRangeException
    {
        public InvalidItemIdException()
        {
        }

        public InvalidItemIdException(string msg) : base(msg)
        {
        }

        protected InvalidItemIdException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
