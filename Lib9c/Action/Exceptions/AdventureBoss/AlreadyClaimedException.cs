using System;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    [Serializable]
    public class AlreadyClaimedException : Exception
    {
        public AlreadyClaimedException(string msg) : base(msg)
        {
        }
    }
}
