#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public sealed class UnbondLockIn : IBencodable, IEquatable<UnbondLockIn>
    {
        private FungibleAssetValue? _releasedFAV;

        public UnbondLockIn(Address address, int maxEntries)
            : this(
                  address,
                  maxEntries,
                  ImmutableSortedDictionary<long, ImmutableList<UnbondLockInEntry>>.Empty)
        {
        }

        public UnbondLockIn(Address address, int maxEntries, IValue bencoded)
            : this(address, maxEntries, (List)bencoded)
        {
        }

        public UnbondLockIn(Address address, int maxEntries, List bencoded)
            : this(
                  address,
                  maxEntries,
                  bencoded.Select(kv => kv is List list
                      ? new KeyValuePair<long, ImmutableList<UnbondLockInEntry>>(
                          (Integer)list[0],
                          ((List)list[1]).Select(e => new UnbondLockInEntry(e)).ToImmutableList())
                      : throw new InvalidCastException(
                          $"Unable to cast object of type '{kv.GetType()}' " +
                          $"to type '{typeof(List)}'."))
                  .ToImmutableSortedDictionary())
        {
        }

        public UnbondLockIn(
            Address address, int maxEntries, IEnumerable<UnbondLockInEntry> entries)
            : this(address, maxEntries)
        {
            foreach (var entry in entries)
            {
                AddEntry(entry);
            }
        }

        private UnbondLockIn(
            Address address,
            int maxEntries,
            ImmutableSortedDictionary<long, ImmutableList<UnbondLockInEntry>> entries,
            FungibleAssetValue? releasedFAV = null)
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
            _releasedFAV = releasedFAV;
        }

        public Address Address { get; }

        public int MaxEntries { get; }

        public bool IsFull => Entries.Values.Sum(e => e.Count) >= MaxEntries;

        public bool IsEmpty => Entries.IsEmpty;

        public ImmutableSortedDictionary<long, ImmutableList<UnbondLockInEntry>> Entries { get; }

        public ImmutableArray<UnbondLockInEntry> FlattenedEntries
            => Entries.Values.SelectMany(e => e).ToImmutableArray();

        public List Bencoded
            => new List(
                Entries.Select(
                    sortedDict => new List(
                        (Integer)sortedDict.Key,
                        new List(sortedDict.Value.Select(e => e.Bencoded)))));

        IValue IBencodable.Bencoded => Bencoded;

        public UnbondLockIn Release(long height)
        {
            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height),
                    height,
                    "The height must be greater than zero.");
            }

            var updatedEntries = Entries;
            FungibleAssetValue? releasedFAV = null;
            foreach (var (expireHeight, entries) in updatedEntries)
            {
                if (expireHeight <= height)
                {
                    FungibleAssetValue entriesFAV = entries
                        .Select(e => e.LockInFAV)
                        .Aggregate((accum, next) => accum + next);
                    releasedFAV = _releasedFAV is null
                        ? entriesFAV
                        : _releasedFAV + entriesFAV;
                    updatedEntries = updatedEntries.Remove(expireHeight);
                }
                else
                {
                    break;
                }
            }

            return UpdateEntries(updatedEntries, releasedFAV);
        }

        [Obsolete("This method is not implemented yet.")]
        public UnbondLockIn Slash()
            => throw new NotImplementedException();

        public FungibleAssetValue? FlushReleasedFAV()
        {
            var releasedFAV = _releasedFAV;
            _releasedFAV = null;
            return releasedFAV;
        }

        public override bool Equals(object? obj)
            => obj is UnbondLockIn other && Equals(other);

        public bool Equals(UnbondLockIn? other)
            => ReferenceEquals(this, other)
            || (other is UnbondLockIn unbondLockIn
            && Address.Equals(unbondLockIn.Address)
            && MaxEntries == unbondLockIn.MaxEntries
            && FlattenedEntries.SequenceEqual(unbondLockIn.FlattenedEntries));

        public override int GetHashCode()
            => Address.GetHashCode();

        internal UnbondLockIn LockIn(
            FungibleAssetValue lockInFAV, long creationHeight, long expireHeight)
        {
            if (expireHeight == creationHeight)
            {
                return this;
            }

            return AddEntry(new UnbondLockInEntry(lockInFAV, creationHeight, expireHeight));
        }

        internal UnbondLockIn Cancel(FungibleAssetValue cancellingFAV, long height)
        {
            if (cancellingFAV.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cancellingFAV),
                    cancellingFAV,
                    "The cancelling FAV must be greater than zero.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height),
                    height,
                    "The height must be greater than zero.");
            }

            if (Cancellable(height) < cancellingFAV)
            {
                throw new InvalidOperationException("Cannot cancel more than locked-in FAV.");
            }

            var updatedEntries = Entries;
            foreach (var (expireHeight, entries) in updatedEntries.Reverse())
            {
                if (expireHeight <= height)
                {
                    throw new InvalidOperationException("Cannot cancel released undelegation.");
                }

                foreach (var entry in entries.Select((value, index) => (value, index)).Reverse())
                {
                    if (cancellingFAV.Sign == 0)
                    {
                        break;
                    }

                    if (entry.value.LockInFAV <= cancellingFAV)
                    {
                        cancellingFAV -= entry.value.LockInFAV; ;
                        updatedEntries = updatedEntries.SetItem(
                            expireHeight,
                            updatedEntries[expireHeight].RemoveAt(entry.index));
                    }
                    else
                    {
                        var cancelledEntry = entry.value.Cancel(cancellingFAV);
                        cancellingFAV -= entry.value.LockInFAV; ;
                        updatedEntries = updatedEntries.SetItem(
                            expireHeight,
                            updatedEntries[expireHeight].SetItem(entry.index, cancelledEntry));
                    }
                }

                if (updatedEntries[expireHeight].IsEmpty)
                {
                    updatedEntries = updatedEntries.Remove(expireHeight);
                }
            }

            return UpdateEntries(updatedEntries);
        }

        internal FungibleAssetValue Cancellable(long height)
            => Entries
                .Where(kv => kv.Key > height)
                .SelectMany(kv => kv.Value)
                .Select(e => e.LockInFAV)
                .Aggregate((accum, next) => accum + next);

        private UnbondLockIn UpdateEntries(
            ImmutableSortedDictionary<long, ImmutableList<UnbondLockInEntry>> entries,
            FungibleAssetValue? releasedFAV=null)
            => new UnbondLockIn(Address, MaxEntries, entries, releasedFAV);

        private UnbondLockIn AddEntry(UnbondLockInEntry entry)
        {
            if (IsFull)
            {
                throw new InvalidOperationException("Cannot add more entries.");
            }

            if (Entries.TryGetValue(entry.ExpireHeight, out var entries))
            {
                return UpdateEntries(Entries.SetItem(entry.ExpireHeight, entries.Add(entry)));
            }
            else
            {
                return UpdateEntries(
                    Entries.Add(
                        entry.ExpireHeight, ImmutableList<UnbondLockInEntry>.Empty.Add(entry)));
            }
        }

        public class UnbondLockInEntry : IBencodable, IEquatable<UnbondLockInEntry>
        {
            private int? _cachedHashCode;

            public UnbondLockInEntry(
                FungibleAssetValue lockInFAV,
                long creationHeight,
                long expireHeight)
                : this(lockInFAV, lockInFAV, creationHeight, expireHeight)
            {
            }

            public UnbondLockInEntry(IValue bencoded)
                : this((List)bencoded)
            {
            }

            private UnbondLockInEntry(List bencoded)
                : this(
                      new FungibleAssetValue(bencoded[0]),
                      new FungibleAssetValue(bencoded[1]),
                      (Integer)bencoded[2],
                      (Integer)bencoded[3])
            {
            }

            private UnbondLockInEntry(
                FungibleAssetValue initialLockInFAV,
                FungibleAssetValue lockInFAV,
                long creationHeight,
                long expireHeight)
            {
                if (initialLockInFAV.Sign <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(initialLockInFAV),
                        initialLockInFAV,
                        "The initial lock-in FAV must be greater than zero.");
                }

                if (lockInFAV.Sign <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(lockInFAV),
                        lockInFAV,
                        "The lock-in FAV must be greater than zero.");
                }

                if (lockInFAV > initialLockInFAV)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(lockInFAV),
                        lockInFAV,
                        "The lock-in FAV must be less than or equal to the initial lock-in FAV.");
                }

                if (creationHeight < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(creationHeight),
                        creationHeight,
                        "The creation height must be greater than or equal to zero.");
                }

                if (expireHeight <= creationHeight)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(expireHeight),
                        expireHeight,
                        "The expire height must be greater than the creation height.");
                }

                InitialLockInFAV = initialLockInFAV;
                LockInFAV = lockInFAV;
                CreationHeight = creationHeight;
                ExpireHeight = expireHeight;
            }

            public FungibleAssetValue InitialLockInFAV { get; }

            public FungibleAssetValue LockInFAV { get; }

            public long CreationHeight { get; }

            public long ExpireHeight { get; }

            public List Bencoded => List.Empty
                .Add(InitialLockInFAV.Serialize())
                .Add(LockInFAV.Serialize())
                .Add(CreationHeight)
                .Add(ExpireHeight);

            IValue IBencodable.Bencoded => Bencoded;

            public override bool Equals(object? obj)
                => obj is UnbondLockInEntry other && Equals(other);

            public bool Equals(UnbondLockInEntry? other)
                => ReferenceEquals(this, other)
                || (other is UnbondLockInEntry unbondLockInEntry
                && InitialLockInFAV.Equals(unbondLockInEntry.InitialLockInFAV)
                && LockInFAV.Equals(unbondLockInEntry.LockInFAV)
                && CreationHeight == unbondLockInEntry.CreationHeight
                && ExpireHeight == unbondLockInEntry.ExpireHeight);

            public override int GetHashCode()
            {
                if (_cachedHashCode is int cached)
                {
                    return cached;
                }

                int hash = HashCode.Combine(
                    InitialLockInFAV,
                    LockInFAV,
                    CreationHeight,
                    ExpireHeight);

                _cachedHashCode = hash;
                return hash;
            }

            [Obsolete("This method is not implemented yet.")]
            public UnbondLockInEntry Slash()
                => throw new NotImplementedException();

            internal UnbondLockInEntry Cancel(FungibleAssetValue cancellingFAV)
            {
                if (cancellingFAV.Sign <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(cancellingFAV),
                        cancellingFAV,
                        "The cancelling FAV must be greater than zero.");
                }

                if (LockInFAV <= cancellingFAV)
                {
                    throw new InvalidOperationException("Cannot cancel more than locked-in FAV.");
                }

                return new UnbondLockInEntry(
                    InitialLockInFAV - cancellingFAV,
                    LockInFAV - cancellingFAV,
                    CreationHeight,
                    ExpireHeight);
            }
        }
    }
}
