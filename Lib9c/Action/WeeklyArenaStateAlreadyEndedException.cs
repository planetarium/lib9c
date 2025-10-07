using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class WeeklyArenaStateAlreadyEndedException : Exception
    {
        public const string BaseMessage = "Aborted as the weekly arena state already ended.";

        public WeeklyArenaStateAlreadyEndedException(string message) : base(message)
        {
        }

        public WeeklyArenaStateAlreadyEndedException() : base(BaseMessage)
        {
        }

        public WeeklyArenaStateAlreadyEndedException(
            SerializationInfo info,
            StreamingContext context) : base (info, context)
        {
        }
    }
}
