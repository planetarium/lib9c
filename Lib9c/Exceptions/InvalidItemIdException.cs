using System;
using System.Runtime.Serialization;

namespace Nekoyume.Exceptions
{
    [Serializable]
    public class InvalidItemIdException : Exception
    {
        public InvalidItemIdException()
        {
        }

        public InvalidItemIdException(string message)
            : base(message)
        {
        }

        public InvalidItemIdException(
            string message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        public InvalidItemIdException(
            string actionType,
            string addressesHex,
            int itemId)
            : base($"[{actionType}][{addressesHex}] Item ID {itemId} not found in any item sheet")
        {
        }

        protected InvalidItemIdException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
