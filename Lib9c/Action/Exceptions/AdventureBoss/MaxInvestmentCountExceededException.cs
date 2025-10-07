using System;
using System.Runtime.Serialization;

namespace Lib9c.Action.Exceptions.AdventureBoss
{
    [Serializable]
    public class MaxInvestmentCountExceededException : Exception
    {
        public MaxInvestmentCountExceededException()
        {
        }

        protected MaxInvestmentCountExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public MaxInvestmentCountExceededException(string message) : base(message)
        {
        }
    }
}
