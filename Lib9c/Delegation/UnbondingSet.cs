#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Nekoyume.Delegation
{
    public sealed class UnbondingSet : IBencodable
    {
        private readonly IDelegationRepository _repository;
        private ImmutableSortedDictionary<UnbondingRef, long> _lowestExpireHeights;

        public UnbondingSet(IDelegationRepository repository)
            : this(
                  ImmutableSortedDictionary<long, ImmutableSortedSet<UnbondingRef>>.Empty,
                  ImmutableSortedDictionary<UnbondingRef, long>.Empty,
                  repository)
        {
        }

        public UnbondingSet(IValue bencoded, IDelegationRepository repository)
            : this((List)bencoded, repository)
        {
        }

        public UnbondingSet(List bencoded, IDelegationRepository repository)
            : this(
                ((List)bencoded[0]).Select(
                    kv => new KeyValuePair<long, ImmutableSortedSet<UnbondingRef>>(
                        (Integer)((List)kv)[0],
                        ((List)((List)kv)[1]).Select(a => new UnbondingRef(a)).ToImmutableSortedSet()))
                  .ToImmutableSortedDictionary(),
                ((List)bencoded[1]).Select(
                    kv => new KeyValuePair<UnbondingRef, long>(
                        new UnbondingRef(((List)kv)[0]),
                        (Integer)((List)kv)[1]))
                  .ToImmutableSortedDictionary(),
                repository)
        {
        }

        private UnbondingSet(
            ImmutableSortedDictionary<long, ImmutableSortedSet<UnbondingRef>> unbondings,
            ImmutableSortedDictionary<UnbondingRef, long> lowestExpireHeights,
            IDelegationRepository repository)
        {
            UnbondingRefs = unbondings;
            _lowestExpireHeights = lowestExpireHeights;
            _repository = repository;
        }

        public static Address Address => new Address(
            ImmutableArray.Create<byte>(
                0x44, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x55));

        public ImmutableSortedDictionary<long, ImmutableSortedSet<UnbondingRef>> UnbondingRefs { get; }

        public ImmutableArray<UnbondingRef> FlattenedUnbondingRefs
            => UnbondingRefs.Values.SelectMany(e => e).ToImmutableArray();

        public IDelegationRepository Repository => _repository;

        public List Bencoded
            => List.Empty
                .Add(new List(
                    UnbondingRefs.Select(
                        sortedDict => new List(
                            (Integer)sortedDict.Key,
                            new List(sortedDict.Value.Select(a => a.Bencoded))))))
                .Add(new List(
                    _lowestExpireHeights.Select(
                        sortedDict => new List(
                            sortedDict.Key.Bencoded,
                            (Integer)sortedDict.Value))));

        IValue IBencodable.Bencoded => Bencoded;

        public bool IsEmpty => UnbondingRefs.IsEmpty;

        public ImmutableArray<IUnbonding> UnbondingsToRelease(long height)
            => UnbondingRefs
                .TakeWhile(kv => kv.Key <= height)
                .SelectMany(kv => kv.Value)
                .Select(unbondingRef => UnbondingFactory.GetUnbondingFromRef(unbondingRef, _repository))
                .ToImmutableArray();

        public UnbondingSet SetUnbondings(IEnumerable<IUnbonding> unbondings)
        {
            UnbondingSet result = this;
            foreach (var unbonding in unbondings)
            {
                result = SetUnbonding(unbonding);
            }

            return result;
        }

        public UnbondingSet SetUnbonding(IUnbonding unbonding)
        {
            if (unbonding.IsEmpty)
            {
                try
                {
                    return RemoveUnbonding(unbonding);
                }
                catch (ArgumentException)
                {
                    return this;
                }          
            }

            UnbondingRef unbondigRef = UnbondingFactory.ToReference(unbonding);

            if (_lowestExpireHeights.TryGetValue(unbondigRef, out var lowestExpireHeight))
            {
                if (lowestExpireHeight == unbonding.LowestExpireHeight)
                {
                    return this;
                }

                var refs = UnbondingRefs[lowestExpireHeight];
                return new UnbondingSet(
                    UnbondingRefs.SetItem(
                        unbonding.LowestExpireHeight,
                        refs.Add(unbondigRef)),
                    _lowestExpireHeights.SetItem(
                        unbondigRef, unbonding.LowestExpireHeight),
                    _repository);
            }

            return new UnbondingSet(
                UnbondingRefs.SetItem(
                    unbonding.LowestExpireHeight,
                    ImmutableSortedSet<UnbondingRef>.Empty.Add(unbondigRef)),
                _lowestExpireHeights.SetItem(
                    unbondigRef, unbonding.LowestExpireHeight),
                _repository);
        }

        private UnbondingSet RemoveUnbonding(IUnbonding unbonding)
        {
            UnbondingRef unbondigRef = UnbondingFactory.ToReference(unbonding);

            if (_lowestExpireHeights.TryGetValue(unbondigRef, out var expireHeight)
                && UnbondingRefs.TryGetValue(expireHeight, out var refs))
            {
                return new UnbondingSet(
                    UnbondingRefs.SetItem(expireHeight, refs.Remove(unbondigRef)),
                    _lowestExpireHeights.Remove(unbondigRef),
                    _repository);
            }
            else
            {
                throw new ArgumentException("The address is not in the unbonding set.");
            }
        }
    }
}
