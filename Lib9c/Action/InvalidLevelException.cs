using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class InvalidLevelException : InvalidOperationException
    {
        public InvalidLevelException(string s) : base(s)
        {
        }

        protected InvalidLevelException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
