using System;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    [Serializable]
    public class PreviousBountyException : Exception
    {
        public PreviousBountyException(string msg) : base(msg)
        {
        }
    }
}
