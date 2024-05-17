using System;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    public class InvalidBountyException : Exception
    {
        public InvalidBountyException(string msg) : base(msg)
        {
        }
    }
}
