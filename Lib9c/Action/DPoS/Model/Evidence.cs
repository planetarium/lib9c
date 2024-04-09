using System.Numerics;
using Bencodex.Types;
using Nekoyume.Action.DPoS.Util;
using Libplanet.Crypto;

namespace Nekoyume.Action.DPoS.Model
{
    public class Evidence
    {
        public Evidence()
        {
        }

        public Evidence(IValue serialized)
        {
            var dict = (Bencodex.Types.Dictionary)serialized;
            Height = dict["height"].ToLong();
            Power = dict["power"].ToBigInteger();
            Address = dict["address"].ToAddress();
        }

        public long Height { get; set; }

        public BigInteger Power { get; set; }

        public Address Address { get; set; }

        public static Address DeriveAddress(Address validatorAddress)
        {
            return validatorAddress.Derive(nameof(Evidence));
        }

        public IValue Serialize()
        {
            return Dictionary.Empty
                .Add("height", Height.Serialize())
                .Add("power", Power.Serialize())
                .Add("address", Address.Serialize());
        }
    }
}
