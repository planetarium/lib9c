using System;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    [Serializable]
    public class InvalidBountyException : Exception
    {
        public InvalidBountyException(string msg) : base(msg)
        {
        }
    }
}
