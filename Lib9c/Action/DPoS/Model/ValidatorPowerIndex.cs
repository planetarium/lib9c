using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action.DPoS.Misc;

namespace Nekoyume.Action.DPoS.Model
{
    public class ValidatorPowerIndex
    {
        public ValidatorPowerIndex()
        {
            Index = new SortedSet<ValidatorPower>();
        }

        public ValidatorPowerIndex(IValue serialized)
        {
            IEnumerable<ValidatorPower> items
                = ((List)serialized).Select(item => new ValidatorPower(item));
            Index = new SortedSet<ValidatorPower>(items);
        }

        public ValidatorPowerIndex(ValidatorPowerIndex consensusPowerIndexInfo)
        {
            Index = consensusPowerIndexInfo.Index;
        }

        public SortedSet<ValidatorPower> Index { get; set; }

        public Address Address => ReservedAddress.ValidatorPowerIndex;

#pragma warning disable S2365
        public List<Address> ValidatorAddresses
            => Index.Select(key => key.ValidatorAddress).ToList();
#pragma warning restore S2365

        public IValue Serialize()
            => new List(Index.Select(consensusPowerKey => consensusPowerKey.Serialize()));
    }
}
