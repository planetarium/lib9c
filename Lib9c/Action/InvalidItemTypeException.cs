using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class InvalidItemTypeException : InvalidOperationException
    {
        public InvalidItemTypeException(string msg) : base(msg)
        {
        }

        protected InvalidItemTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
