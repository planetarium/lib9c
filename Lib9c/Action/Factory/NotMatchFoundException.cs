using System;
using Libplanet.Action;

namespace Nekoyume.Action.Factory
{
    public class NotMatchFoundException : Exception
    {
        public NotMatchFoundException(string message) : base(message)
        {
        }

        public NotMatchFoundException(string message, Exception innerException) :
            base(message, innerException)
        {
        }

        public NotMatchFoundException(Type type, long blockIndex) :
            base($"There is no matching action for {type.Name}" +
                 $" at block index {blockIndex}.")
        {
        }

        public NotMatchFoundException(Type type, string actionType) :
            base($"There is no matching action for {type.Name}" +
                 $" with action type \"{actionType}\".")
        {
        }
    }
}
