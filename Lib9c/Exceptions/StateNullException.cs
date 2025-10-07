#nullable enable

using System;
using System.Runtime.Serialization;
using Libplanet.Crypto;

namespace Lib9c.Exceptions
{
    [Serializable]
    public class StateNullException : Exception
    {
        public StateNullException()
        {
        }

        public StateNullException(string? message) : base(message)
        {
        }

        public StateNullException(
            string? message,
            Exception? innerException)
            : base(message, innerException)
        {
        }

        public StateNullException(
            Address accountAddress,
            Address address,
            Exception? innerException = null)
            : base($"State is null or Null.Value: {accountAddress}.{address}", innerException)
        {
        }

        protected StateNullException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
