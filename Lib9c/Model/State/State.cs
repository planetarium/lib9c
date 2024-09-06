using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Crypto;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.State
{
    [Serializable]
    public abstract class State : IState
    {
        public Address address;

        protected State(Address address)
        {
            this.address = address;
        }

        protected State(IValue iValue)
            : this(iValue switch
            {
                Dictionary dict => dict.ContainsKey(LegacyAddressKey)
                    ? dict[LegacyAddressKey].ToAddress()
                    : dict[AddressKey].ToAddress(),
                List list => list[0].ToAddress(),
                _ => throw new ArgumentException($"{iValue} is not a dictionary.")
            })
        {
        }

        protected IValue SerializeBase() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)LegacyAddressKey] = address.Serialize(),
            });
        protected IValue SerializeV2Base() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)AddressKey] = address.Serialize(),
            });

        protected IValue SerializeListBase() =>
            new List(address.Serialize());
    }
}
