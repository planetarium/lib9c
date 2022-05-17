using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    public class NotEnoughMedalException : Exception
    {
        public NotEnoughMedalException(string msg) : base(msg)
        {
        }

        public NotEnoughMedalException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
