using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class NotEnoughWeeklyArenaChallengeCountException : Exception
    {
        public const string BaseMessage = "Aborted as the arena state reached the daily limit.";

        public NotEnoughWeeklyArenaChallengeCountException(string message) : base(message)
        {
        }

        public NotEnoughWeeklyArenaChallengeCountException() : base(BaseMessage)
        {
        }

        public NotEnoughWeeklyArenaChallengeCountException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
