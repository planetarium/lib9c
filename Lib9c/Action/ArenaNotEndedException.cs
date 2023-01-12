using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class ArenaNotEndedException : InvalidOperationException
    {
        public ArenaNotEndedException(string s) : base(s)
        {
        }

        protected ArenaNotEndedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
