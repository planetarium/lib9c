using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class ProductNotFoundException : InvalidOperationException
    {
        public ProductNotFoundException(string msg) : base(msg)
        {
        }

        public ProductNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
