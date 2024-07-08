using System;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    [Serializable]
    public class SeasonInProgressException : Exception
    {
        public SeasonInProgressException(string msg) : base(msg)
        {
        }
    }
}
