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
    public sealed class RebondGrace : IBencodable, IEquatable<RebondGrace>
    {
        public RebondGrace(Address address, int maxEntries)
            : this(
                  address,
                  maxEntries,
                  ImmutableSortedDictionary<long, ImmutableList<RebondGraceEntry>>.Empty)
        {
        }

        public RebondGrace(Address address, int maxEntries, IValue bencoded)
            : this(address, maxEntries, (List)bencoded)
        {
        }

        public RebondGrace(Address address, int maxEntries, List bencoded)
            : this(
                  address,
                  maxEntries,
                  bencoded.Select(kv => kv is List list
                      ? new KeyValuePair<long, ImmutableList<RebondGraceEntry>>(
                          (Integer)list[0],
                          ((List)list[1]).Select(e => new RebondGraceEntry(e)).ToImmutableList())
                      : throw new InvalidCastException(
                          $"Unable to cast object of type '{kv.GetType()}' to type '{typeof(List)}'."))
                  .ToImmutableSortedDictionary())
        {
        }

        public RebondGrace(Address address, int maxEntries, IEnumerable<RebondGraceEntry> entries)
            : this(address, maxEntries)
        {
            foreach (var entry in entries)
            {
                AddEntry(entry);
            }
        }

        private RebondGrace(
            Address address,
            int maxEntries,
            ImmutableSortedDictionary<long, ImmutableList<RebondGraceEntry>> entries)
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
        }

        public Address Address { get; }

        public int MaxEntries { get; }

        public bool IsFull => Entries.Values.Sum(e => e.Count) >= MaxEntries;

        public bool IsEmpty => Entries.IsEmpty;

        public ImmutableSortedDictionary<long, ImmutableList<RebondGraceEntry>> Entries { get; }

        public ImmutableArray<RebondGraceEntry> FlattenedEntries
            => Entries.Values.SelectMany(e => e).ToImmutableArray();

        public IValue Bencoded
            => new List(
                Entries.Select(
                    sortedDict => new List(
                        (Integer)sortedDict.Key,
                        new List(sortedDict.Value.Select(e => e.Bencoded)))));

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

        [Obsolete("This method is not implemented yet.")]
        public RebondGrace Slash()
            => throw new NotImplementedException();

        public override bool Equals(object obj)
            => obj is RebondGrace other && Equals(other);

        public bool Equals(RebondGrace other)
            => ReferenceEquals(this, other)
            || (Address.Equals(other.Address)
            && MaxEntries == other.MaxEntries
            && FlattenedEntries.SequenceEqual(other.FlattenedEntries));

        public override int GetHashCode()
            => Address.GetHashCode();

        internal RebondGrace Grace(
            Address rebondeeAddress, FungibleAssetValue initialGraceFAV, long creationHeight, long expireHeight)
        {
            if (expireHeight == creationHeight)
            {
                return this;
            }

            return AddEntry(new RebondGraceEntry(rebondeeAddress, initialGraceFAV, creationHeight, expireHeight));
        }

        private RebondGrace AddEntry(RebondGraceEntry entry)
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
                return UpdateEntries(Entries.Add(entry.ExpireHeight, ImmutableList<RebondGraceEntry>.Empty.Add(entry)));
            }
        }

        private RebondGrace UpdateEntries(
            ImmutableSortedDictionary<long, ImmutableList<RebondGraceEntry>> entries)
            => new RebondGrace(Address, MaxEntries, entries);

        public class RebondGraceEntry : IBencodable, IEquatable<RebondGraceEntry>
        {
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

                if (expireHeight <= creationHeight)
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

            public IValue Bencoded => List.Empty
                .Add(RebondeeAddress.Bencoded)
                .Add(InitialGraceFAV.Serialize())
                .Add(GraceFAV.Serialize())
                .Add(CreationHeight)
                .Add(ExpireHeight);

            public bool Equals(RebondGraceEntry other)
                => ReferenceEquals(this, other)
                || (RebondeeAddress.Equals(other.RebondeeAddress)
                && InitialGraceFAV.Equals(other.InitialGraceFAV)
                && GraceFAV.Equals(other.GraceFAV)
                && CreationHeight == other.CreationHeight
                && ExpireHeight == other.ExpireHeight);

            [Obsolete("This method is not implemented yet.")]
            public RebondGraceEntry Slash()
                => throw new NotImplementedException();
        }
    }
}
