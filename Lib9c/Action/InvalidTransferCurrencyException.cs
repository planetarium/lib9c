using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class InvalidTransferCurrencyException : InvalidOperationException
    {
        protected InvalidTransferCurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public InvalidTransferCurrencyException(string message) : base(message)
        {
        }
    }
}
