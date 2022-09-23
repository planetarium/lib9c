using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
{
    public class NotEnoughHammerPointException : Exception
    {
        public NotEnoughHammerPointException(string s) : base(s)
        {
        }

        protected NotEnoughHammerPointException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
}
