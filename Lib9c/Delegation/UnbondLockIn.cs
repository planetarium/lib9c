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
    public sealed class UnbondLockIn : IUnbonding, IBencodable, IEquatable<UnbondLockIn>
    {
        private static readonly IComparer<UnbondLockInEntry> _entryComparer
            = new UnbondLockInEntryComparer();

        private readonly IDelegationRepository? _repository;

        public UnbondLockIn(
            Address address,
            int maxEntries,
            Address sender,
            Address recipient,
            IDelegationRepository? repository)
            : this(
                  address,
                  maxEntries,
                  sender,
                  recipient,
                  ImmutableSortedDictionary<long, ImmutableList<UnbondLockInEntry>>.Empty,
                  repository)
        {
            _repository = repository;
        }

        public UnbondLockIn(
            Address address, int maxEntries, IValue bencoded, IDelegationRepository? repository = null)
            : this(address, maxEntries, (List)bencoded, repository)
        {
        }

        public UnbondLockIn(
            Address address, int maxEntries, List bencoded, IDelegationRepository? repository = null)
            : this(
                  address,
                  maxEntries,
                  new Address(bencoded[0]),
                  new Address(bencoded[1]),
                  ((List)bencoded[2]).Select(kv => kv is List list
                      ? new KeyValuePair<long, ImmutableList<UnbondLockInEntry>>(
                          (Integer)list[0],
                          ((List)list[1]).Select(e => new UnbondLockInEntry(e)).ToImmutableList())
                      : throw new InvalidCastException(
                          $"Unable to cast object of type '{kv.GetType()}' " +
                          $"to type '{typeof(List)}'."))
                  .ToImmutableSortedDictionary(),
                  repository)
        {
        }

        public UnbondLockIn(
            Address address,
            int maxEntries,
            Address sender,
            Address recipient,
            IEnumerable<UnbondLockInEntry> entries,
            IDelegationRepository? repository = null)
            : this(address, maxEntries, sender, recipient, repository)
        {
            foreach (var entry in entries)
            {
                AddEntry(entry);
            }
        }

        private UnbondLockIn(
            Address address,
            int maxEntries,
            Address sender,
            Address recipient,
            ImmutableSortedDictionary<long, ImmutableList<UnbondLockInEntry>> entries,
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
            Sender = sender;
            Recipient = recipient;
            _repository = repository;
        }

        public Address Address { get; }

        public int MaxEntries { get; }

        public Address Sender { get; }

        public Address Recipient { get; }

        // TODO: Use better custom collection type
        public ImmutableSortedDictionary<long, ImmutableList<UnbondLockInEntry>> Entries { get; }

        public long LowestExpireHeight => Entries.First().Key;

        public bool IsFull => Entries.Values.Sum(e => e.Count) >= MaxEntries;

        public bool IsEmpty => Entries.IsEmpty;

        public ImmutableArray<UnbondLockInEntry> FlattenedEntries
            => Entries.Values.SelectMany(e => e).ToImmutableArray();

        public List Bencoded
            => List.Empty
                .Add(Sender.Bencoded)
                .Add(Recipient.Bencoded)
                .Add(new List(
                    Entries.Select(
                        sortedDict => new List(
                            (Integer)sortedDict.Key,
                            new List(sortedDict.Value.Select(e => e.Bencoded))))));

        IValue IBencodable.Bencoded => Bencoded;

        public UnbondLockIn Release(long height)
        {
            CannotMutateRelationsWithoutRepository();
            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height),
                    height,
                    "The height must be greater than zero.");
            }

            var updatedEntries = Entries;
            FungibleAssetValue? releasingFAV = null;
            foreach (var (expireHeight, entries) in updatedEntries)
            {
                if (expireHeight <= height)
                {
                    FungibleAssetValue entriesFAV = entries
                        .Select(e => e.LockInFAV)
                        .Aggregate((accum, next) => accum + next);
                    releasingFAV = releasingFAV.HasValue
                        ? releasingFAV.Value + entriesFAV
                        : entriesFAV;
                    updatedEntries = updatedEntries.Remove(expireHeight);
                    
                }
                else
                {
                    break;
                }
            }

            if (releasingFAV.HasValue)
            {
                _repository!.TransferAsset(Sender, Recipient, releasingFAV.Value);
            }

            return UpdateEntries(updatedEntries);
        }

        IUnbonding IUnbonding.Release(long height) => Release(height);

        public UnbondLockIn Slash(BigInteger slashFactor, long infractionHeight)
            => UpdateEntries(Entries.TakeWhile(e => e.Key >= infractionHeight)
                .Select(kv => KeyValuePair.Create(
                    kv.Key,
                    kv.Value.Select(v => v.Slash(slashFactor, infractionHeight)).ToImmutableList()))
                .Concat(Entries.SkipWhile(e => e.Key >= infractionHeight))
                .ToImmutableSortedDictionary());

        IUnbonding IUnbonding.Slash(BigInteger slashFactor, long infractionHeight)
            => Slash(slashFactor, infractionHeight);

        public override bool Equals(object? obj)
            => obj is UnbondLockIn other && Equals(other);

        public bool Equals(UnbondLockIn? other)
            => ReferenceEquals(this, other)
            || (other is UnbondLockIn unbondLockIn
            && Address.Equals(unbondLockIn.Address)
            && MaxEntries == unbondLockIn.MaxEntries
            && Sender.Equals(unbondLockIn.Sender)
            && Recipient.Equals(unbondLockIn.Recipient)
            && FlattenedEntries.SequenceEqual(unbondLockIn.FlattenedEntries));

        public override int GetHashCode()
            => Address.GetHashCode();

        internal UnbondLockIn LockIn(
            FungibleAssetValue lockInFAV, long creationHeight, long expireHeight)
        {
            if (expireHeight < creationHeight)
            {
                throw new ArgumentException("The expire height must be greater than the creation height.");
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
                        cancellingFAV -= entry.value.LockInFAV;
                        updatedEntries = updatedEntries.SetItem(
                            expireHeight,
                            updatedEntries[expireHeight].RemoveAt(entry.index));
                    }
                    else
                    {
                        var cancelledEntry = entry.value.Cancel(cancellingFAV);
                        cancellingFAV -= entry.value.LockInFAV;
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
            ImmutableSortedDictionary<long, ImmutableList<UnbondLockInEntry>> entries)
            => new UnbondLockIn(Address, MaxEntries, Sender, Recipient, entries, _repository);

        private UnbondLockIn AddEntry(UnbondLockInEntry entry)
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
                    entry.ExpireHeight, ImmutableList<UnbondLockInEntry>.Empty.Add(entry)));
        }

        private void CannotMutateRelationsWithoutRepository()
        {
            if (_repository is null)
            {
                throw new InvalidOperationException(
                    "Cannot mutate without repository.");
            }
        }

        public class UnbondLockInEntry : IUnbondingEntry, IBencodable, IEquatable<UnbondLockInEntry>
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

                if (expireHeight < creationHeight)
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

            public UnbondLockInEntry Slash(BigInteger slashFactor, long infractionHeight)
            {
                if (CreationHeight > infractionHeight ||
                    ExpireHeight < infractionHeight)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(infractionHeight),
                        infractionHeight,
                        "The infraction height must be between in creation height and expire height of entry.");
                }

                return new UnbondLockInEntry(
                    InitialLockInFAV,
                    LockInFAV - LockInFAV.DivRem(slashFactor).Quotient,
                    CreationHeight,
                    ExpireHeight);
            }

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

        public class UnbondLockInEntryComparer : IComparer<UnbondLockInEntry>
        {
            public int Compare(UnbondLockInEntry? x, UnbondLockInEntry? y)
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

                comparison = -x.InitialLockInFAV.CompareTo(y.InitialLockInFAV);
                if (comparison != 0)
                {
                    return comparison;
                }

                return -x.LockInFAV.CompareTo(y.LockInFAV);
            }
        }
    }
}
