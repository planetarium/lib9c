using System;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    [Serializable]
    public class InsufficientStakingException : Exception
    {
        public InsufficientStakingException(string msg) : base(msg)
        {
        }
    }
}
