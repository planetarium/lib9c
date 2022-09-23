using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
{
    public abstract class ActivationException : Exception
    {
        protected ActivationException()
        {
        }
        
        protected ActivationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
