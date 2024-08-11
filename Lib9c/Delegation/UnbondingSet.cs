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

        private ImmutableSortedDictionary<Address, long> _lowestExpireHeights;
        private ImmutableSortedDictionary<Address, byte[]> _typeDict;

        public UnbondingSet()
            : this(
                  ImmutableSortedDictionary<long, ImmutableSortedSet<Address>>.Empty,
                  ImmutableSortedDictionary<Address, long>.Empty,
                  ImmutableSortedDictionary<Address, byte[]>.Empty)
        {
        }

        public UnbondingSet(IValue bencoded)
            : this((List)bencoded)
        {
        }

        public UnbondingSet(List bencoded)
            : this(
                ((List)bencoded[1]).Select(
                    kv => new KeyValuePair<long, ImmutableSortedSet<Address>>(
                        (Integer)((List)kv)[0],
                        ((List)((List)kv)[1]).Select(a => new Address(a)).ToImmutableSortedSet()))
                  .ToImmutableSortedDictionary(),
                ((List)bencoded[2]).Select(
                    kv => new KeyValuePair<Address, long>(
                        new Address(((List)kv)[0]),
                        (Integer)((List)kv)[1]))
                  .ToImmutableSortedDictionary(),
                ((List)bencoded[2]).Select(
                    kv => new KeyValuePair<Address, byte[]>(
                        new Address(((List)kv)[0]),
                        ((Binary)((List)kv)[1]).ToArray()))
                  .ToImmutableSortedDictionary())
        {
        }

        private UnbondingSet(
            ImmutableSortedDictionary<long, ImmutableSortedSet<Address>> unbondings,
            ImmutableSortedDictionary<Address, long> lowestExpireHeights,
            ImmutableSortedDictionary<Address, byte[]> typeDict)
        {
            Unbondings = unbondings;
            _lowestExpireHeights = lowestExpireHeights;
            _typeDict = typeDict;
        }

        public static Address Address => new Address(
            ImmutableArray.Create<byte>(
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00));

        public ImmutableSortedDictionary<long, ImmutableSortedSet<Address>> Unbondings { get; }

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
                        unbonding.Address, ToTypeBytes(unbonding)));
            }

            return new UnbondingSet(
                Unbondings.SetItem(
                    unbonding.LowestExpireHeight,
                    ImmutableSortedSet<Address>.Empty.Add(unbonding.Address)),
                _lowestExpireHeights.SetItem(
                    unbonding.Address, unbonding.LowestExpireHeight),
                _typeDict.SetItem(
                    unbonding.Address, ToTypeBytes(unbonding)));
        }

        public UnbondingSet RemoveUnbonding(Address address)
        {
            if (_lowestExpireHeights.TryGetValue(address, out var expireHeight)
                && Unbondings.TryGetValue(expireHeight, out var addresses))
            {
                return new UnbondingSet(
                    Unbondings.SetItem(expireHeight, addresses.Remove(address)),
                    _lowestExpireHeights.Remove(address),
                    _typeDict.Remove(address));
            }
            else
            {
                throw new ArgumentException("The address is not in the unbonding set.");
            }
        }

        public IUnbonding[] ReleaseUnbondings(long height, Func<Address, Type, IValue> bencodedGetter)
        {
            return Unbondings
                .TakeWhile(kv => kv.Key <= height)
                .SelectMany(kv => kv.Value)
                .Select(address => (
                    Address: address,
                    Type: ToUnbondingType(_typeDict[address])))
                .Select(tuple => LoadUnbonding(
                    tuple.Address,
                    tuple.Type,
                    bencodedGetter(tuple.Address, tuple.Type)))
                .Select(u => u.Release(height)).ToArray();
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

        private static IUnbonding LoadUnbonding(Address address, Type type, IValue bencoded)
            => type switch
        {
            var t when t == typeof(UnbondLockIn) => new UnbondLockIn(address, int.MaxValue, bencoded),
            var t when t == typeof(RebondGrace) => new RebondGrace(address, int.MaxValue, bencoded),
            _ => throw new ArgumentException("Invalid unbonding type.")
        };
    }
}
