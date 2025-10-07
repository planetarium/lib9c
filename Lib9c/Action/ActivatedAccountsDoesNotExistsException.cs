using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class ActivatedAccountsDoesNotExistsException : ActivationException
    {
        public ActivatedAccountsDoesNotExistsException()
        {
        }

        public ActivatedAccountsDoesNotExistsException(string message) : base(message)
        {
        }

        public ActivatedAccountsDoesNotExistsException(
            SerializationInfo info, StreamingContext context
        ) : base(info, context)
        {
        }
    }
}
