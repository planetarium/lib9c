using System;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    [Serializable]
    public class MaxInvestmentCountExceededException : Exception
    {
        public MaxInvestmentCountExceededException(string message) : base(message)
        {
        }
    }
}
