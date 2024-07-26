using System;
using System.Runtime.Serialization;

namespace Nekoyume.Delegation
{
    [Serializable]
    public class UnbondException : Exception
    {
        public UnbondException()
        {
        }

        public UnbondException(string message)
            : base(message)
        {
        }

        public UnbondException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UnbondException(
            SerializationInfo info,
            StreamingContext context
        )
            : base(info, context)
        {
        }
    }
}
