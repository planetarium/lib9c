using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class ActionUnavailableException : InvalidOperationException
    {
        public ActionUnavailableException()
        {
        }

        public ActionUnavailableException(string s) : base(s)
        {
        }

        protected ActionUnavailableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
