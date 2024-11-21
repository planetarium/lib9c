using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
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
