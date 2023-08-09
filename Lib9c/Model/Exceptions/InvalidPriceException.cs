using System;
using System.Runtime.Serialization;

namespace Nekoyume.Model.Exceptions
{
    [Serializable]
    public class InvalidPriceException : ArgumentOutOfRangeException
    {
        public InvalidPriceException(string msg) : base(msg)
        {
        }

        protected InvalidPriceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
