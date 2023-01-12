using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class OrderIdDoesNotExistException : InvalidOperationException
    {
        public OrderIdDoesNotExistException(string msg) : base(msg)
        {
        }

        protected OrderIdDoesNotExistException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
