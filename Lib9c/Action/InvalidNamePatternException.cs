using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class InvalidNamePatternException : InvalidOperationException
    {
        public InvalidNamePatternException(string s) : base(s)
        {
        }

        protected InvalidNamePatternException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
