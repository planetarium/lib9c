using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using MessagePack;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.State
{
    [Serializable]
    [MessagePackObject]
    [Union(0, typeof(PendingActivationState))]
    public abstract class State : IState
    {
        [Key(0)]
#pragma warning disable MsgPack003
        public Address address;
#pragma warning restore MsgPack003

        protected State(Address address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            this.address = address;
        }

        protected State(Dictionary serialized)
            : this(serialized.ContainsKey(LegacyAddressKey)
                ? serialized[LegacyAddressKey].ToAddress()
                : serialized[AddressKey].ToAddress())
        {
        }

        protected State(IValue iValue) : this((Dictionary)iValue)
        {
        }

        public virtual IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)LegacyAddressKey] = address.Serialize(),
            });
        public virtual IValue SerializeV2() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)AddressKey] = address.Serialize(),
            });

    }
}
