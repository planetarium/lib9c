using System;

namespace Nekoyume.Action.Exceptions.AdventureBoss
{
    public class MaxInvestmentCountExceededException : Exception
    {
        public MaxInvestmentCountExceededException(string message) : base(message)
        {
        }
    }
}
