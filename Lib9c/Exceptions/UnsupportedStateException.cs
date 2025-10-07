using System;
using System.Runtime.Serialization;

namespace Lib9c.Exceptions
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

        public UnsupportedStateException(
            int expectedVersion,
            int actualVersion,
            Exception innerException = null) :
            base(
                "Unsupported state version." +
                $" Expected: {expectedVersion}, Actual: {actualVersion}.",
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
