using System;

namespace Nekoyume.Action.Exceptions
{
    [Serializable]
    public class EmptyRewardException : Exception
    {
        public EmptyRewardException(string msg) : base(msg)
        {
        }
    }
}
