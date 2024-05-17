using System;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    public class InsufficientStakingException : Exception
    {
        public InsufficientStakingException(string msg) : base(msg)
        {
        }
    }
}
