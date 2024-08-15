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
        private static readonly byte[] _unbondLockInTypeBytes = new byte[] { 0x75 }; // 'u'
        private static readonly byte[] _rebondGraceTypeBytes = new byte[] { 0x72 };  // 'r'

        private readonly IDelegationRepository _repository;
        private ImmutableSortedDictionary<Address, long> _lowestExpireHeights;
        private ImmutableSortedDictionary<Address, byte[]> _typeDict;

        public UnbondingSet(IDelegationRepository repository)
            : this(
                  ImmutableSortedDictionary<long, ImmutableSortedSet<Address>>.Empty,
                  ImmutableSortedDictionary<Address, long>.Empty,
                  ImmutableSortedDictionary<Address, byte[]>.Empty,
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
                    kv => new KeyValuePair<long, ImmutableSortedSet<Address>>(
                        (Integer)((List)kv)[0],
                        ((List)((List)kv)[1]).Select(a => new Address(a)).ToImmutableSortedSet()))
                  .ToImmutableSortedDictionary(),
                ((List)bencoded[1]).Select(
                    kv => new KeyValuePair<Address, long>(
                        new Address(((List)kv)[0]),
                        (Integer)((List)kv)[1]))
                  .ToImmutableSortedDictionary(),
                ((List)bencoded[2]).Select(
                    kv => new KeyValuePair<Address, byte[]>(
                        new Address(((List)kv)[0]),
                        ((Binary)((List)kv)[1]).ToArray()))
                  .ToImmutableSortedDictionary(),
                repository)
        {
        }

        private UnbondingSet(
            ImmutableSortedDictionary<long, ImmutableSortedSet<Address>> unbondings,
            ImmutableSortedDictionary<Address, long> lowestExpireHeights,
            ImmutableSortedDictionary<Address, byte[]> typeDict,
            IDelegationRepository repository)
        {
            Unbondings = unbondings;
            _lowestExpireHeights = lowestExpireHeights;
            _typeDict = typeDict;
            _repository = repository;
        }

        public static Address Address => new Address(
            ImmutableArray.Create<byte>(
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00));

        public ImmutableSortedDictionary<long, ImmutableSortedSet<Address>> Unbondings { get; }

        public ImmutableArray<Address> FlattenedUnbondings
            => Unbondings.Values.SelectMany(e => e).ToImmutableArray();

        public IDelegationRepository Repository => _repository;

        public List Bencoded
            => List.Empty
                .Add(new List(
                    Unbondings.Select(
                        sortedDict => new List(
                            (Integer)sortedDict.Key,
                            new List(sortedDict.Value.Select(a => a.Bencoded))))))
                .Add(new List(
                    _lowestExpireHeights.Select(
                        sortedDict => new List(
                            sortedDict.Key.Bencoded,
                            (Integer)sortedDict.Value))))
                .Add(new List(
                    _typeDict.Select(
                        sortedDict => new List(
                            sortedDict.Key.Bencoded,
                            (Binary)sortedDict.Value))));

        IValue IBencodable.Bencoded => Bencoded;

        public bool IsEmpty => Unbondings.IsEmpty;

        public ImmutableArray<IUnbonding> UnbondingsToRelease(long height)
            => Unbondings
                .TakeWhile(kv => kv.Key <= height)
                .SelectMany(kv => kv.Value)
                .Select(address => (
                    Address: address,
                    Type: ToUnbondingType(_typeDict[address])))
                .Select(tuple => LoadUnbonding(
                    tuple.Address,
                    tuple.Type))
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
                return RemoveUnbonding(unbonding.Address);
            }

            if (_lowestExpireHeights.TryGetValue(unbonding.Address, out var lowestExpireHeight))
            {
                if (lowestExpireHeight == unbonding.LowestExpireHeight)
                {
                    return this;
                }

                var addresses = Unbondings[lowestExpireHeight];
                return new UnbondingSet(
                    Unbondings.SetItem(
                        unbonding.LowestExpireHeight,
                        addresses.Add(unbonding.Address)),
                    _lowestExpireHeights.SetItem(
                        unbonding.Address, unbonding.LowestExpireHeight),
                    _typeDict.SetItem(
                        unbonding.Address, ToTypeBytes(unbonding)),
                    _repository);
            }

            return new UnbondingSet(
                Unbondings.SetItem(
                    unbonding.LowestExpireHeight,
                    ImmutableSortedSet<Address>.Empty.Add(unbonding.Address)),
                _lowestExpireHeights.SetItem(
                    unbonding.Address, unbonding.LowestExpireHeight),
                _typeDict.SetItem(
                    unbonding.Address, ToTypeBytes(unbonding)),
                _repository);
        }


        private UnbondingSet RemoveUnbonding(Address address)
        {
            if (_lowestExpireHeights.TryGetValue(address, out var expireHeight)
                && Unbondings.TryGetValue(expireHeight, out var addresses))
            {
                return new UnbondingSet(
                    Unbondings.SetItem(expireHeight, addresses.Remove(address)),
                    _lowestExpireHeights.Remove(address),
                    _typeDict.Remove(address),
                    _repository);
            }
            else
            {
                throw new ArgumentException("The address is not in the unbonding set.");
            }
        }

        private static byte[] ToTypeBytes(IUnbonding unbonding)
            => unbonding switch
        {
            UnbondLockIn _ => _unbondLockInTypeBytes,
            RebondGrace _ => _rebondGraceTypeBytes,
            _ => throw new ArgumentException("Invalid unbonding type.")
        };

        private static Type ToUnbondingType(byte[] typeBytes) => typeBytes switch
        {
            _ when typeBytes.SequenceEqual(_unbondLockInTypeBytes)
                => typeof(UnbondLockIn),
            _ when typeBytes.SequenceEqual(_rebondGraceTypeBytes)
                => typeof(RebondGrace),
            _ => throw new ArgumentException("Invalid type bytes.")
        };

        private IUnbonding LoadUnbonding(Address address, Type type)
            => type switch
        {
            var t when t == typeof(UnbondLockIn) => _repository.GetUnlimitedUnbondLockIn(address),
            var t when t == typeof(RebondGrace) => _repository.GetUnlimitedRebondGrace(address),
            _ => throw new ArgumentException("Invalid unbonding type.")
        };
    }
}
