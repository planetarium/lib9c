using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class RequiredBlockIndexException : Exception
    {
        public RequiredBlockIndexException()
        {
        }

        public RequiredBlockIndexException(string msg) : base(msg)
        {
        }

        public RequiredBlockIndexException(
            string actionType,
            string addressesHex,
            long currentBlockIndex)
            : base($"[{actionType}][{addressesHex}] current({currentBlockIndex})")
        {
        }

        public RequiredBlockIndexException(
            string actionType,
            string addressesHex,
            long requiredBlockIndex,
            long currentBlockIndex)
            : base($"[{actionType}][{addressesHex}] current({currentBlockIndex}) < " +
                   $"required({requiredBlockIndex})")
        {
        }

        protected RequiredBlockIndexException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
