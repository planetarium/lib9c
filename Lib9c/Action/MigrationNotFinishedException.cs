using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class MigrationNotFinishedException : InvalidOperationException
    {
        public MigrationNotFinishedException()
        {
        }

        public MigrationNotFinishedException(string s) : base(s)
        {
        }

        protected MigrationNotFinishedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
