using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
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
