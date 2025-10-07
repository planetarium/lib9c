using System;
using System.Runtime.Serialization;
using Libplanet.Types.Assets;

namespace Lib9c.Action
{
    [Serializable]
    public class TotalSupplyDoesNotExistException : Exception
    {
        public Currency Currency { get; }

        public TotalSupplyDoesNotExistException(Currency currency)
        {
            Currency = currency;
        }

        public TotalSupplyDoesNotExistException(string message) : base(message)
        {
        }

        protected TotalSupplyDoesNotExistException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
            Currency = (Currency)info.GetValue(nameof(Currency), typeof(Currency));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Currency), Currency);
        }
    }
}
