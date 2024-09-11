using System;
using System.Runtime.Serialization;

namespace Nekoyume.Exceptions
{
    [Serializable]
    public class UnsupportedStateException : Exception
    {
        public UnsupportedStateException(
            string expectedName,
            int expectedVersion,
            string actualName,
            int actualVersion,
            Exception innerException = null) :
            base(
                "Unsupported state." +
                $" Expected: {expectedName}({expectedVersion}), Actual: {actualName}({actualVersion}).",
                innerException)
        {
        }

        public UnsupportedStateException(string msg) : base(msg)
        {
        }

        protected UnsupportedStateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
