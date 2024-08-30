#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public sealed class RebondGrace : IUnbonding, IBencodable, IEquatable<RebondGrace>
    {
        private static readonly IComparer<RebondGraceEntry> _entryComparer
            = new RebondGraceEntryComparer();

        private readonly IDelegationRepository? _repository;

        public RebondGrace(Address address, int maxEntries, IDelegationRepository? repository = null)
            : this(
                  address,
                  maxEntries,
                  ImmutableSortedDictionary<long, ImmutableList<RebondGraceEntry>>.Empty,
                  repository)
        {
        }

        public RebondGrace(Address address, int maxEntries, IValue bencoded, IDelegationRepository? repository = null)
            : this(address, maxEntries, (List)bencoded, repository)
        {
        }

        public RebondGrace(Address address, int maxEntries, List bencoded, IDelegationRepository? repository = null)
            : this(
                  address,
                  maxEntries,
                  bencoded.Select(kv => kv is List list
                      ? new KeyValuePair<long, ImmutableList<RebondGraceEntry>>(
                          (Integer)list[0],
                          ((List)list[1]).Select(e => new RebondGraceEntry(e)).ToImmutableList())
                      : throw new InvalidCastException(
                          $"Unable to cast object of type '{kv.GetType()}' to type '{typeof(List)}'."))
                  .ToImmutableSortedDictionary(),
                  repository)
        {
        }

        public RebondGrace(
            Address address,
            int maxEntries,
            IEnumerable<RebondGraceEntry> entries,
            IDelegationRepository? repository = null)
            : this(address, maxEntries, repository)
        {
            foreach (var entry in entries)
            {
                AddEntry(entry);
            }
        }

        private RebondGrace(
            Address address,
            int maxEntries,
            ImmutableSortedDictionary<long, ImmutableList<RebondGraceEntry>> entries,
            IDelegationRepository? repository)
        {
            if (maxEntries < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxEntries),
                    maxEntries,
                    "The max entries must be greater than or equal to zero.");
            }

            Address = address;
            MaxEntries = maxEntries;
            Entries = entries;
            _repository = repository;
        }

        public Address Address { get; }

        public int MaxEntries { get; }

        public IDelegationRepository? Repository => _repository;

        public long LowestExpireHeight => Entries.First().Key;

        public bool IsFull => Entries.Values.Sum(e => e.Count) >= MaxEntries;

        public bool IsEmpty => Entries.IsEmpty;

        // TODO: Use better custom collection type
        public ImmutableSortedDictionary<long, ImmutableList<RebondGraceEntry>> Entries { get; }

        public ImmutableArray<RebondGraceEntry> FlattenedEntries
            => Entries.Values.SelectMany(e => e).ToImmutableArray();

        public List Bencoded
            => new List(
                Entries.Select(
                    sortedDict => new List(
                        (Integer)sortedDict.Key,
                        new List(sortedDict.Value.Select(e => e.Bencoded)))));

        IValue IBencodable.Bencoded => Bencoded;

        public RebondGrace Release(long height)
        {
            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height),
                    height,
                    "The height must be greater than zero.");
            }

            var updatedEntries = Entries;
            foreach (var (expireHeight, entries) in updatedEntries)
            {
                if (expireHeight <= height)
                {
                    updatedEntries = updatedEntries.Remove(expireHeight);
                }
                else
                {
                    break;
                }
            }

            return UpdateEntries(updatedEntries);
        }

        IUnbonding IUnbonding.Release(long height) => Release(height);

        public RebondGrace Slash(BigInteger slashFactor, long infractionHeight)
            => UpdateEntries(Entries.TakeWhile(e => e.Key >= infractionHeight)
                .Select(kv => KeyValuePair.Create(
                    kv.Key,
                    kv.Value.Select(v => v.Slash(slashFactor, infractionHeight)).ToImmutableList()))
                .Concat(Entries.SkipWhile(e => e.Key >= infractionHeight))
                .ToImmutableSortedDictionary());

        IUnbonding IUnbonding.Slash(BigInteger slashFactor, long infractionHeight)
            => Slash(slashFactor, infractionHeight);

        public override bool Equals(object? obj)
            => obj is RebondGrace other && Equals(other);

        public bool Equals(RebondGrace? other)
            => ReferenceEquals(this, other)
            || (other is RebondGrace rebondGrace
            && Address.Equals(rebondGrace.Address)
            && MaxEntries == rebondGrace.MaxEntries
            && FlattenedEntries.SequenceEqual(rebondGrace.FlattenedEntries));

        public override int GetHashCode()
            => Address.GetHashCode();

        internal RebondGrace Grace(
            Address rebondeeAddress,
            FungibleAssetValue initialGraceFAV,
            long creationHeight,
            long expireHeight)
        {
            if (expireHeight < creationHeight)
            {
                throw new ArgumentException(
                    "The expire height must be greater than the creation height.");
            }

            return AddEntry(
                new RebondGraceEntry(
                    rebondeeAddress, initialGraceFAV, creationHeight, expireHeight));
        }

        private RebondGrace AddEntry(RebondGraceEntry entry)
        {
            if (IsFull)
            {
                throw new InvalidOperationException("Cannot add more entries.");
            }

            if (Entries.TryGetValue(entry.ExpireHeight, out var entries))
            {
                int index = entries.BinarySearch(entry, _entryComparer);
                return UpdateEntries(
                    Entries.SetItem(
                        entry.ExpireHeight,
                        entries.Insert(index < 0 ? ~index : index, entry)));
            }

            return UpdateEntries(
                Entries.Add(
                    entry.ExpireHeight, ImmutableList<RebondGraceEntry>.Empty.Add(entry)));
        }

        private RebondGrace UpdateEntries(
            ImmutableSortedDictionary<long, ImmutableList<RebondGraceEntry>> entries)
            => new RebondGrace(Address, MaxEntries, entries, _repository);

        private void CannotMutateRelationsWithoutRepository()
        {
            if (_repository is null)
            {
                throw new InvalidOperationException(
                    "Cannot mutate without repository.");
            }
        }

        public class RebondGraceEntry : IUnbondingEntry, IBencodable, IEquatable<RebondGraceEntry>
        {
            private int? _cachedHashCode;

            public RebondGraceEntry(
                Address rebondeeAddress,
                FungibleAssetValue graceFAV,
                long creationHeight,
                long expireHeight)
                : this(rebondeeAddress, graceFAV, graceFAV, creationHeight, expireHeight)
            {
            }

            public RebondGraceEntry(IValue bencoded)
                : this((List)bencoded)
            {
            }

            private RebondGraceEntry(List bencoded)
                : this(
                      new Address(bencoded[0]),
                      new FungibleAssetValue(bencoded[1]),
                      new FungibleAssetValue(bencoded[2]),
                      (Integer)bencoded[3],
                      (Integer)bencoded[4])
            {
            }

            private RebondGraceEntry(
                Address rebondeeAddress,
                FungibleAssetValue initialGraceFAV,
                FungibleAssetValue graceFAV,
                long creationHeight,
                long expireHeight)
            {
                if (initialGraceFAV.Sign <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(initialGraceFAV),
                        initialGraceFAV,
                        "The initial grace FAV must be greater than zero.");
                }

                if (graceFAV.Sign <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(graceFAV),
                        graceFAV,
                        "The grace FAV must be greater than zero.");
                }

                if (graceFAV > initialGraceFAV)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(graceFAV),
                        graceFAV,
                        "The grace FAV must be less than or equal to the initial grace FAV.");
                }

                if (creationHeight < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(creationHeight),
                        creationHeight,
                        "The creation height must be greater than or equal to zero.");
                }

                if (expireHeight < creationHeight)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(expireHeight),
                        expireHeight,
                        "The expire height must be greater than the creation height.");
                }

                RebondeeAddress = rebondeeAddress;
                InitialGraceFAV = initialGraceFAV;
                GraceFAV = graceFAV;
                CreationHeight = creationHeight;
                ExpireHeight = expireHeight;
            }

            public Address RebondeeAddress { get; }

            public FungibleAssetValue InitialGraceFAV { get; }

            public FungibleAssetValue GraceFAV { get; }

            public long CreationHeight { get; }

            public long ExpireHeight { get; }

            public List Bencoded => List.Empty
                .Add(RebondeeAddress.Bencoded)
                .Add(InitialGraceFAV.Serialize())
                .Add(GraceFAV.Serialize())
                .Add(CreationHeight)
                .Add(ExpireHeight);

            IValue IBencodable.Bencoded => Bencoded;

            public override bool Equals(object? obj)
                => obj is RebondGraceEntry other && Equals(other);

            public bool Equals(RebondGraceEntry? other)
                => ReferenceEquals(this, other)
                || (other is RebondGraceEntry rebondGraceEntry
                && RebondeeAddress.Equals(rebondGraceEntry.RebondeeAddress)
                && InitialGraceFAV.Equals(rebondGraceEntry.InitialGraceFAV)
                && GraceFAV.Equals(rebondGraceEntry.GraceFAV)
                && CreationHeight == rebondGraceEntry.CreationHeight
                && ExpireHeight == rebondGraceEntry.ExpireHeight);

            public override int GetHashCode()
            {
                if (_cachedHashCode is int cached)
                {
                    return cached;
                }

                int hash = HashCode.Combine(
                    RebondeeAddress,
                    InitialGraceFAV,
                    GraceFAV,
                    CreationHeight,
                    ExpireHeight);

                _cachedHashCode = hash;
                return hash;
            }

            public RebondGraceEntry Slash(BigInteger slashFactor, long infractionHeight)
            {
                if (CreationHeight > infractionHeight ||
                    ExpireHeight < infractionHeight)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(infractionHeight),
                        infractionHeight,
                        "The infraction height must be between in creation height and expire height of entry.");
                }

                return new RebondGraceEntry(
                    RebondeeAddress,
                    GraceFAV - GraceFAV.DivRem(slashFactor).Quotient,
                    CreationHeight,
                    ExpireHeight);
            }
        }

        public class RebondGraceEntryComparer : IComparer<RebondGraceEntry>
        {
            public int Compare(RebondGraceEntry? x, RebondGraceEntry? y)
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

                int comparison = x.ExpireHeight.CompareTo(y.ExpireHeight);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = x.CreationHeight.CompareTo(y.CreationHeight);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = -x.InitialGraceFAV.CompareTo(y.InitialGraceFAV);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = -x.GraceFAV.CompareTo(y.GraceFAV);
                if (comparison != 0)
                {
                    return comparison;
                }

                return x.RebondeeAddress.CompareTo(y.RebondeeAddress);
            }
        }
    }
}
