#nullable enable
using System.Collections.Immutable;
using System.Linq;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Nekoyume.Delegation
{
    public sealed class UnbondingSet : IBencodable
    {
        public UnbondingSet()
        {
            UnbondLockIns = ImmutableSortedSet<Address>.Empty;
            RebondGraces = ImmutableSortedSet<Address>.Empty;
        }

        public UnbondingSet(IValue bencoded)
            : this((List)bencoded)
        {
        }

        public UnbondingSet(List bencoded)
            : this(
                ((List)bencoded[0]).Select(e => new Address(e)).ToImmutableSortedSet(),
                ((List)bencoded[1]).Select(e => new Address(e)).ToImmutableSortedSet())
        {
        }

        private UnbondingSet(
            ImmutableSortedSet<Address> unbondLockIns,
            ImmutableSortedSet<Address> rebondGraces)
        {
            UnbondLockIns = unbondLockIns;
            RebondGraces = rebondGraces;
        }

        public static Address Address => new Address(
            ImmutableArray.Create<byte>(
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00));

        public ImmutableSortedSet<Address> UnbondLockIns { get; }

        public ImmutableSortedSet<Address> RebondGraces { get; }

        public List Bencoded
            => List.Empty
                .Add(new List(UnbondLockIns.Select(a => a.Bencoded)))
                .Add(new List(RebondGraces.Select(a => a.Bencoded)));

        IValue IBencodable.Bencoded => Bencoded;

        public UnbondingSet AddUnbondLockIn(Address address)
            => new UnbondingSet(UnbondLockIns.Add(address), RebondGraces);

        public UnbondingSet RemoveUnbondLockIn(Address address)
            => new UnbondingSet(UnbondLockIns.Remove(address), RebondGraces);

        public UnbondingSet AddRebondGrace(Address address)
            => new UnbondingSet(UnbondLockIns, RebondGraces.Add(address));

        public UnbondingSet RemoveRebondGrace(Address address)
            => new UnbondingSet(UnbondLockIns, RebondGraces.Remove(address));
    }
}
