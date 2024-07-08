using System;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    [Serializable]
    public class ClaimExpiredException : Exception
    {
        public ClaimExpiredException(string msg) : base(msg)
        {
        }
    }
}
