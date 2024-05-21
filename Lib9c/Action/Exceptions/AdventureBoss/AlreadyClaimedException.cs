using System;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    public class AlreadyClaimedException : Exception
    {
        public AlreadyClaimedException(string msg) : base(msg)
        {
        }
    }
}
