using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidItemCountException : ArgumentOutOfRangeException
    {
        public InvalidItemCountException()
        {
        }

        public InvalidItemCountException(string msg) : base(msg)
        {
        }

        protected InvalidItemCountException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
