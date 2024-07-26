using System;
using System.Runtime.Serialization;

namespace Nekoyume.Delegation
{
    [Serializable]
    public class BondException : Exception
    {
        public BondException()
        {
        }

        public BondException(string message)
            : base(message)
        {
        }

        public BondException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BondException(
            SerializationInfo info,
            StreamingContext context
        )
            : base(info, context)
        {
        }
    }
}
