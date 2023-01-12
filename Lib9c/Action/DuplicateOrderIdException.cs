using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class DuplicateOrderIdException : InvalidOperationException
    {
        public DuplicateOrderIdException(string s) : base(s)
        {
        }

        protected DuplicateOrderIdException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
