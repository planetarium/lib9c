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

        private UnbondingSet(ImmutableSortedSet<Address> unbondLockIns, ImmutableSortedSet<Address> rebondGraces)
        {
            UnbondLockIns = unbondLockIns;
            RebondGraces = rebondGraces;
        }


        public ImmutableSortedSet<Address> UnbondLockIns { get; private set; }

        public ImmutableSortedSet<Address> RebondGraces { get; private set; }

        public void AddUnbondLockIn(Address address)
        {
            UnbondLockIns = UnbondLockIns.Add(address);
        }

        public void RemoveUnbondLockIn(Address address)
        {
            UnbondLockIns = UnbondLockIns.Remove(address);
        }

        public void AddRebondGrace(Address address)
        {
            RebondGraces = RebondGraces.Add(address);
        }

        public void RemoveRebondGrace(Address address)
        {
            RebondGraces = RebondGraces.Remove(address);
        }

        public IValue Bencoded
            => List.Empty
                .Add(new List(UnbondLockIns.Select(a => a.Bencoded)))
                .Add(new List(RebondGraces.Select(a => a.Bencoded)));
    }
}
