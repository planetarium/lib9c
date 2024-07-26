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
    public class UnbondLockIn : IBencodable
    {
        public UnbondLockIn(Address address, int maxEntries)
        {
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

        public IValue Bencoded
            => new List(
                Entries.Select(
                    sortedDict => new List(
                        (Integer)sortedDict.Key,
                        new List(sortedDict.Value.Select(e => e.Bencoded)))));

        public void LockIn(FungibleAssetValue lockInFAV, long creationHeight, long expireHeight)
            => AddEntry(new UnbondLockInEntry(lockInFAV, creationHeight, expireHeight));

        public void Cancel(FungibleAssetValue cancellingFAV, long height)
        {
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

        public FungibleAssetValue? Release(long height)
        {
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

        public class UnbondLockInEntry : IBencodable
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

            public void Cancel(FungibleAssetValue cancellingFAV)
            {
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
