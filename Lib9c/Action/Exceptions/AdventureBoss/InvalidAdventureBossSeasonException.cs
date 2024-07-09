using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    [Serializable]
    public class InvalidAdventureBossSeasonException : Exception
    {
        public InvalidAdventureBossSeasonException()
        {
        }

        protected InvalidAdventureBossSeasonException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public InvalidAdventureBossSeasonException(string message) : base(message)
        {
        }
    }
}
