using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
{
    [Serializable]
    public class ActionObsoletedException : InvalidOperationException
    {
        public ActionObsoletedException()
        {
        }

        public ActionObsoletedException(string s) : base(s)
        {
        }

        protected ActionObsoletedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
