using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    public class NotEnoughWinException : Exception
    {
        public NotEnoughWinException(string msg) : base(msg)
        {
        }

        public NotEnoughWinException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
