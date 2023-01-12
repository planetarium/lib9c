using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class ActionPointExceededException : InvalidOperationException
    {
        public ActionPointExceededException()
        {
        }

        public ActionPointExceededException(string s) : base(s)
        {
        }

        protected ActionPointExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
