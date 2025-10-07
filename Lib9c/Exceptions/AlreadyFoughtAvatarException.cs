using System;

namespace Lib9c.Exceptions
{
    [Serializable]
    public class AlreadyFoughtAvatarException : Exception
    {
        public AlreadyFoughtAvatarException()
        {
        }

        public AlreadyFoughtAvatarException(string message) : base(message)
        {
        }
    }
}
