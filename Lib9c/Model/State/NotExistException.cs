using System;
using System.Runtime.Serialization;

namespace Nekoyume.Model.State
{
    public class NotExistException : Exception
    {
        public NotExistException(string message) : base(message)
        {
        }

        protected NotExistException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
