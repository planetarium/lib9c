using System;
using System.Runtime.Serialization;

namespace Nekoyume.Model.Exceptions
{
    [Serializable]
    public class InvalidProductTypeException : InvalidOperationException
    {
        public InvalidProductTypeException(string msg) : base(msg)
        {
        }

        protected InvalidProductTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
