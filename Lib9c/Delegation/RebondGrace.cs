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
    public class RebondGrace : IBencodable
    {
        public RebondGrace(Address address, int maxEntries)
        {
            Address = address;
            MaxEntries = maxEntries;
            Entries = ImmutableSortedDictionary<long, ImmutableList<RebondGraceEntry>>.Empty;
        }

        public RebondGrace(Address address, int maxEntries, IValue bencoded)
            : this(address, maxEntries, (List)bencoded)
        {
        }

        public RebondGrace(Address address, int maxEntries, List bencoded)
        {
            Address = address;
            MaxEntries = maxEntries;
            Entries = bencoded
                .Select(kv => kv is List list
                    ? new KeyValuePair<long, ImmutableList<RebondGraceEntry>>(
                        (Integer)list[0],
                        ((List)list[1]).Select(e => new RebondGraceEntry(e)).ToImmutableList())
                    : throw new InvalidCastException(
                        $"Unable to cast object of type '{kv.GetType()}' to type '{typeof(List)}'."))
                .ToImmutableSortedDictionary();
        }

        public RebondGrace(Address address, int maxEntries, IEnumerable<RebondGraceEntry> entries)
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

        public ImmutableSortedDictionary<long, ImmutableList<RebondGraceEntry>> Entries { get; private set; }


        public IValue Bencoded
            => new List(
                Entries.Select(
                    sortedDict => new List(
                        (Integer)sortedDict.Key,
                        new List(sortedDict.Value.Select(e => e.Bencoded)))));

        public void Grace(Address rebondeeAddress, FungibleAssetValue initialGraceFAV, long creationHeight, long expireHeight)
            => AddEntry(new RebondGraceEntry(rebondeeAddress, initialGraceFAV, creationHeight, expireHeight));

        public void Release(long height)
        {
            foreach (var (expireHeight, entries) in Entries)
            {
                if (expireHeight <= height)
                {
                    Entries = Entries.Remove(expireHeight);
                }
                else
                {
                    break;
                }
            }
        }

        private void AddEntry(RebondGraceEntry entry)
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
                Entries = Entries.Add(entry.ExpireHeight, ImmutableList<RebondGraceEntry>.Empty.Add(entry));
            }
        }

        public class RebondGraceEntry : IBencodable
        {
            public RebondGraceEntry(
                Address rebondeeAddress,
                FungibleAssetValue initialGraceFAV,
                long creationHeight,
                long expireHeight)
            {
                RebondeeAddress = rebondeeAddress;
                InitialGraceFAV = initialGraceFAV;
                GraceFAV = initialGraceFAV;
                CreationHeight = creationHeight;
                ExpireHeight = expireHeight;
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
                RebondeeAddress = rebondeeAddress;
                InitialGraceFAV = initialGraceFAV;
                GraceFAV = graceFAV;
                CreationHeight = creationHeight;
                ExpireHeight = expireHeight;
            }

            public Address RebondeeAddress { get; }

            public FungibleAssetValue InitialGraceFAV { get; }

            public FungibleAssetValue GraceFAV { get; private set; }

            public long CreationHeight { get; }

            public long ExpireHeight { get; }

            public IValue Bencoded => List.Empty
                .Add(RebondeeAddress.Bencoded)
                .Add(InitialGraceFAV.Serialize())
                .Add(GraceFAV.Serialize())
                .Add(CreationHeight)
                .Add(ExpireHeight);
        }
    }
}
