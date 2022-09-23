using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
{
    [Serializable]
    public class AlreadyReceivedException : InvalidOperationException
    {
        public AlreadyReceivedException(string s) : base(s)
        {
        }

        protected AlreadyReceivedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
