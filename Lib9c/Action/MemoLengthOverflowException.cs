using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class MemoLengthOverflowException : InvalidOperationException
    {
        public MemoLengthOverflowException(string message) : base(message)
        {
        }

        protected MemoLengthOverflowException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
