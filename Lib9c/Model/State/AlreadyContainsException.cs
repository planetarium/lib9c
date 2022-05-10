using System;
using System.Runtime.Serialization;

namespace Nekoyume.Model.State
{
    public class AlreadyContainsException : Exception
    {
        public AlreadyContainsException(string message) : base(message)
        {
        }

        protected AlreadyContainsException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
