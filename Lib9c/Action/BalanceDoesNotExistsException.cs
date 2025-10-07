using System;
using System.Runtime.Serialization;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Action
{
    [Serializable]
    public class BalanceDoesNotExistsException : Exception
    {
        public Address Address { get; }
        public Currency Currency { get; }

        public BalanceDoesNotExistsException(Address address, Currency currency)
        {
            Address = address;
            Currency = currency;
        }

        public BalanceDoesNotExistsException(string message) : base(message)
        {
        }

        protected BalanceDoesNotExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Address = (Address)info.GetValue(nameof(Address), typeof(Address));
            Currency = (Currency)info.GetValue(nameof(Currency), typeof(Currency));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Address), Address);
            info.AddValue(nameof(Currency), Currency);
        }
    }
}
