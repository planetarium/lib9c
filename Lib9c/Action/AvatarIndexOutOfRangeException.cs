using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    public class AvatarIndexOutOfRangeException : Exception
    {
        public AvatarIndexOutOfRangeException(string message) : base(message)
        {
        }

        protected AvatarIndexOutOfRangeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
