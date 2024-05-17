using System;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    public class SeasonInProgressException : Exception
    {
        public SeasonInProgressException(string msg) : base(msg)
        {
        }
    }
}
