using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
{
    [Serializable]
    public class AppraiseBlockNotReachedException : Exception
    {
        public AppraiseBlockNotReachedException(string message) : base(message)
        {
        }

        protected AppraiseBlockNotReachedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
