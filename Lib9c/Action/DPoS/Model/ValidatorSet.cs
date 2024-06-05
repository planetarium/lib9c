using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.DPoS.Misc;

namespace Nekoyume.Action.DPoS.Model
{
    public class ValidatorSet
    {
        private readonly SortedSet<ValidatorPower> _set;

        public ValidatorSet()
        {
            _set = new SortedSet<ValidatorPower>();
        }

        public ValidatorSet(IValue serialized)
        {
            IEnumerable<ValidatorPower> validatorPowerEnum
                = ((List)serialized).Select(x => new ValidatorPower(x));
            _set = new SortedSet<ValidatorPower>(validatorPowerEnum);
        }

        public ValidatorSet(ValidatorSet bondedValidatorSet)
        {
            _set = bondedValidatorSet._set;
        }

        public static int MaxBondedSetSize => 100;

        public static Address PreviousBondedAddress => ReservedAddress.PreviousBondedValidatorSet;

        public static Address BondedAddress => ReservedAddress.BondedValidatorSet;

        public static Address UnbondedAddress => ReservedAddress.UnbondedValidatorSet;

        public long Count => _set.Count;

        public ImmutableSortedSet<ValidatorPower> Set => _set.ToImmutableSortedSet();

        public FungibleAssetValue TotalConsensusToken
            => Set.Aggregate(
                Asset.ConsensusToken * 0, (total, next) => total + next.ConsensusToken);

        public ValidatorPower this[int index] => _set.ElementAt(index);

        public void Add(ValidatorPower validatorPower)
        {
            _set.Add(validatorPower);
        }

        public IValue Serialize()
        {
            return new List(_set.Select(x => x.Serialize()));
        }
    }
}
