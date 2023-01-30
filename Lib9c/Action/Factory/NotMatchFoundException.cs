using System;
using Libplanet.Action;

namespace Nekoyume.Action.Factory
{
    public class NotMatchFoundException : Exception
    {
        public NotMatchFoundException(string message, Exception innerException = null) :
            base(message, innerException)
        {
        }

        public NotMatchFoundException(
            Type type,
            long blockIndex,
            Exception innerException = null) :
            this(
                $"There is no matching action for {type.Name}" +
                $" at block index {blockIndex}.",
                innerException)
        {
        }

        public NotMatchFoundException(
            Type type,
            string actionType,
            Exception innerException = null) :
            this(
                $"There is no matching action for {type.Name}" +
                $" with action type \"{actionType}\".",
                innerException)
        {
        }
    }
}
