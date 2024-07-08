using System;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    [Serializable]
    public class InvalidAdventureBossSeasonException : Exception
    {

        public InvalidAdventureBossSeasonException(string message) : base(message)
        {
        }
    }
}
