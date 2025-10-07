using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    public abstract class ActivationException : Exception
    {
        protected ActivationException()
        {
        }

        protected ActivationException(string message) : base(message)
        {
        }

        protected ActivationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
