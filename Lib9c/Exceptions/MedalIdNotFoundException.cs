using System;
using System.Runtime.Serialization;

namespace Lib9c.Exceptions
{
    [Serializable]
    public class MedalIdNotFoundException : Exception
    {
        public MedalIdNotFoundException()
        {
        }

        public MedalIdNotFoundException(string message)
            : base(message)
        {
        }

        public MedalIdNotFoundException(
            string message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        public MedalIdNotFoundException(
            string actionType,
            string addressesHex,
            int requiredAmount,
            int currentAmount)
            : base($"[{actionType}][{addressesHex}] required({requiredAmount}), current({currentAmount})")
        {
        }

        protected MedalIdNotFoundException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
