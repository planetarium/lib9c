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
        public UnbondLockIn(Address address, int maxEntries)
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
            Entries = ImmutableSortedDictionary<long, ImmutableList<UnbondLockInEntry>>.Empty;
        }

        public UnbondLockIn(Address address, int maxEntries, IValue bencoded)
            : this(address, maxEntries, (List)bencoded)
        {
        }

        public UnbondLockIn(Address address, int maxEntries, List bencoded)
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
            Entries = bencoded
                .Select(kv => kv is List list
                    ? new KeyValuePair<long, ImmutableList<UnbondLockInEntry>>(
                        (Integer)list[0],
                        ((List)list[1]).Select(e => new UnbondLockInEntry(e)).ToImmutableList())
                    : throw new InvalidCastException(
                        $"Unable to cast object of type '{kv.GetType()}' to type '{typeof(List)}'."))
                .ToImmutableSortedDictionary();
        }

        public UnbondLockIn(Address address, int maxEntries, IEnumerable<UnbondLockInEntry> entries)
            : this(address, maxEntries)
        {
            foreach (var entry in entries)
            {
                AddEntry(entry);
            }
        }

        public Address Address { get; }

        public int MaxEntries { get; }

        public bool IsFull => Entries.Values.Sum(e => e.Count) >= MaxEntries;

        public bool IsEmpty => Entries.IsEmpty;

        public ImmutableSortedDictionary<long, ImmutableList<UnbondLockInEntry>> Entries { get; private set; }

        public ImmutableArray<UnbondLockInEntry> FlattenedEntries
            => Entries.Values.SelectMany(e => e).ToImmutableArray();

        public IValue Bencoded
            => new List(
                Entries.Select(
                    sortedDict => new List(
                        (Integer)sortedDict.Key,
                        new List(sortedDict.Value.Select(e => e.Bencoded)))));

        public FungibleAssetValue? Release(long height)
        {
            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height),
                    height,
                    "The height must be greater than zero.");
            }

            FungibleAssetValue? releaseFAV = null;
            foreach (var (expireHeight, entries) in Entries)
            {
                if (expireHeight <= height)
                {
                    FungibleAssetValue entriesFAV = entries
                        .Select(e => e.LockInFAV)
                        .Aggregate((accum, next) => accum + next);
                    releaseFAV = releaseFAV is null
                        ? entriesFAV
                        : releaseFAV + entriesFAV;
                    Entries = Entries.Remove(expireHeight);
                }
                else
                {
                    break;
                }
            }

            return releaseFAV;
        }

        [Obsolete("This method is not implemented yet.")]
        public void Slash()
            => throw new NotImplementedException();

        public override bool Equals(object obj)
            => obj is UnbondLockIn other && Equals(other);

        public bool Equals(UnbondLockIn other)
            => ReferenceEquals(this, other)
            || (Address.Equals(other.Address)
            && MaxEntries == other.MaxEntries
            && FlattenedEntries.SequenceEqual(other.FlattenedEntries));

        public override int GetHashCode()
            => Address.GetHashCode();

        internal void LockIn(FungibleAssetValue lockInFAV, long creationHeight, long expireHeight)
        {
            if (expireHeight == creationHeight)
            {
                return;
            }

            AddEntry(new UnbondLockInEntry(lockInFAV, creationHeight, expireHeight));
        }

        internal void Cancel(FungibleAssetValue cancellingFAV, long height)
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

            foreach (var (expireHeight, entries) in Entries.Reverse())
            {
                if (expireHeight <= height)
                {
                    throw new InvalidOperationException("Cannot cancel released undelegation.");
                }

                foreach (var entry in entries.Select((value, index) => (value, index)).Reverse())
                {
                    if (cancellingFAV.RawValue < 0)
                    {
                        throw new InvalidOperationException("Insufficient undelegation to cancel");
                    }

                    if (entry.value.LockInFAV < cancellingFAV)
                    {
                        cancellingFAV -= entry.value.LockInFAV;
                        Entries = Entries.SetItem(
                            expireHeight,
                            Entries[expireHeight].RemoveAt(entry.index));
                    }
                    else
                    {
                        entry.value.Cancel(cancellingFAV);
                        Entries = Entries.SetItem(
                            expireHeight,
                            Entries[expireHeight].SetItem(entry.index, entry.value));
                        break;
                    }
                }

                if (Entries[expireHeight].IsEmpty)
                {
                    Entries = Entries.Remove(expireHeight);
                }
            }
        }

        private FungibleAssetValue Cancellable(long height)
            => - Entries
                .Where(kv => kv.Key > height)
                .SelectMany(kv => kv.Value)
                .Select(e => e.LockInFAV)
                .Aggregate((accum, next) => accum + next);

        private void AddEntry(UnbondLockInEntry entry)
        {
            if (IsFull)
            {
                throw new InvalidOperationException("Cannot add more entries.");
            }

            if (Entries.TryGetValue(entry.ExpireHeight, out var entries))
            {
                Entries = Entries.SetItem(entry.ExpireHeight, entries.Add(entry));
            }
            else
            {
                Entries = Entries.Add(entry.ExpireHeight, ImmutableList<UnbondLockInEntry>.Empty.Add(entry));
            }
        }

        public class UnbondLockInEntry : IBencodable, IEquatable<UnbondLockInEntry>
        {
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

                if (lockInFAV >= initialLockInFAV)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(lockInFAV),
                        lockInFAV,
                        "The lock-in FAV must be less than the initial lock-in FAV.");
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

            public FungibleAssetValue InitialLockInFAV { get; private set; }

            public FungibleAssetValue LockInFAV { get; private set; }

            public long CreationHeight { get; }

            public long ExpireHeight { get; }

            public IValue Bencoded => List.Empty
                .Add(InitialLockInFAV.Serialize())
                .Add(LockInFAV.Serialize())
                .Add(CreationHeight)
                .Add(ExpireHeight);

            public bool Equals(UnbondLockInEntry other)
                => ReferenceEquals(this, other)
                || (InitialLockInFAV.Equals(other.InitialLockInFAV)
                && LockInFAV.Equals(other.LockInFAV)
                && CreationHeight == other.CreationHeight
                && ExpireHeight == other.ExpireHeight);

            internal void Cancel(FungibleAssetValue cancellingFAV)
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

                InitialLockInFAV -= cancellingFAV;
                LockInFAV -= cancellingFAV;
            }
        }
    }
}
