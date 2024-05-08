using System;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    public class ClaimExpiredException : Exception
    {
        public ClaimExpiredException(string msg) : base(msg)
        {
        }
    }
}
