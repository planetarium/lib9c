#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Consensus;

namespace Nekoyume.ValidatorDelegation
{
    public sealed class ValidatorList : IBencodable
    {
        private static readonly IComparer<Validator> _reversedComparer
            = Comparer<Validator>.Create((y, x) => new ValidatorComparer().Compare(x, y));

        public ValidatorList()
            : this(ImmutableList<Validator>.Empty)
        {
        }

        public ValidatorList(IValue bencoded)
            : this((List)bencoded)
        {
        }

        public ValidatorList(List bencoded)
            : this(bencoded.Select(v => new Validator(v)).ToImmutableList())
        {
        }


        private ValidatorList(ImmutableList<Validator> validators)
        {
            Validators = validators;
        }

        public static Address Address => new Address(
            ImmutableArray.Create<byte>(
                0x56, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x56));

        public ImmutableList<Validator> Validators { get; }

        public static int MaxActiveSetSize => 100;

        public List Bencoded => new List(Validators.Select(v => v.Bencoded));

        IValue IBencodable.Bencoded => Bencoded;

        public List<Validator> ActiveSet() => Validators.Take(MaxActiveSetSize).ToList();

        public List<Validator> InActiveSet() => Validators.Skip(MaxActiveSetSize).ToList();

        public ValidatorList SetValidator(Validator validator)
            => RemoveValidator(validator.PublicKey).AddValidator(validator);

        public ValidatorList RemoveValidator(PublicKey publicKey)
            => UpdateValidators(Validators.RemoveAll(v => v.PublicKey.Equals(publicKey)));

        private ValidatorList AddValidator(Validator validator)
        {
            int index = Validators.BinarySearch(validator, _reversedComparer);
            return UpdateValidators(Validators.Insert(index < 0 ? ~index : index, validator));
        }

        private ValidatorList UpdateValidators(ImmutableList<Validator> validators)
            => new ValidatorList(validators);
    }

    public class ValidatorComparer : IComparer<Validator>
    {
        public int Compare(Validator? x, Validator? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            int comparison = x.Power.CompareTo(y.Power);
            if (comparison != 0)
            {
                return comparison;
            }

            return x.OperatorAddress.CompareTo(y.OperatorAddress);
        }
    }
}
