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
        private static readonly IComparer<UnbondingEntry> _entryComparer
            = new UnbondingEntry.Comparer();

        private readonly IDelegationRepository? _repository;

        public RebondGrace(
            Address address,
            int maxEntries,
            Address delegateeAddress,
            Address delegatorAddress,
            IDelegationRepository? repository = null)
            : this(
                  address,
                  maxEntries,
                  delegateeAddress,
                  delegatorAddress,
                  ImmutableSortedDictionary<long, ImmutableList<UnbondingEntry>>.Empty,
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
                  new Address(bencoded[0]),
                  new Address(bencoded[1]),
                  ((List)bencoded[2]).Select(kv => kv is List list
                      ? new KeyValuePair<long, ImmutableList<UnbondingEntry>>(
                          (Integer)list[0],
                          ((List)list[1]).Select(e => new UnbondingEntry(e)).ToImmutableList())
                      : throw new InvalidCastException(
                          $"Unable to cast object of type '{kv.GetType()}' to type '{typeof(List)}'."))
                  .ToImmutableSortedDictionary(),
                  repository)
        {
        }

        public RebondGrace(
            Address address,
            int maxEntries,
            Address delegateeAddress,
            Address delegatorAddress,
            IEnumerable<UnbondingEntry> entries,
            IDelegationRepository? repository = null)
            : this(
                  address,
                  maxEntries,
                  delegateeAddress,
                  delegatorAddress,
                  repository)
        {
            foreach (var entry in entries)
            {
                AddEntry(entry);
            }
        }

        private RebondGrace(
            Address address,
            int maxEntries,
            Address delegateeAddress,
            Address delegatorAddress,
            ImmutableSortedDictionary<long, ImmutableList<UnbondingEntry>> entries,
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
            DelegateeAddress = delegateeAddress;
            DelegatorAddress = delegatorAddress;
            _repository = repository;
        }

        public Address Address { get; }

        public int MaxEntries { get; }

        public Address DelegateeAddress { get; }

        public Address DelegatorAddress { get; }

        public IDelegationRepository? Repository => _repository;

        public long LowestExpireHeight => Entries.First().Key;

        public bool IsFull => Entries.Values.Sum(e => e.Count) >= MaxEntries;

        public bool IsEmpty => Entries.IsEmpty;

        // TODO: Use better custom collection type
        public ImmutableSortedDictionary<long, ImmutableList<UnbondingEntry>> Entries { get; }

        public ImmutableArray<UnbondingEntry> FlattenedEntries
            => Entries.Values.SelectMany(e => e).ToImmutableArray();

        public List Bencoded
            => List.Empty
                .Add(DelegateeAddress.Bencoded)
                .Add(DelegatorAddress.Bencoded)
                .Add(new List(
                    Entries.Select(
                        sortedDict => new List(
                            (Integer)sortedDict.Key,
                            new List(sortedDict.Value.Select(e => e.Bencoded))))));

        IValue IBencodable.Bencoded => Bencoded;

        public RebondGrace Release(long height, out FungibleAssetValue? releasedFAV)
        {
            releasedFAV = null;

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

        IUnbonding IUnbonding.Release(long height, out FungibleAssetValue? releasedFAV) => Release(height, out releasedFAV);

        public RebondGrace Slash(
            BigInteger slashFactor,
            long infractionHeight,
            long height,
            Address slashedPoolAddress)
        {
            // TODO: Extract common logic to abstract class
            CannotMutateRelationsWithoutRepository();

            var slashed = new SortedDictionary<Address, FungibleAssetValue>();
            var updatedEntries = Entries;
            var entriesToSlash = Entries.TakeWhile(e => e.Key >= infractionHeight);
            foreach (var (expireHeight, entries) in entriesToSlash)
            {
                ImmutableList<UnbondingEntry> slashedEntries = ImmutableList<UnbondingEntry>.Empty;
                foreach (var entry in entries)
                {
                    var slashedEntry = entry.Slash(slashFactor, infractionHeight, out var slashedSingle);
                    int index = slashedEntries.BinarySearch(slashedEntry, _entryComparer);
                    slashedEntries = slashedEntries.Insert(index < 0 ? ~index : index, slashedEntry);
                    if (slashed.TryGetValue(entry.UnbondeeAddress, out var value))
                    {
                        slashed[entry.UnbondeeAddress] = value + slashedSingle;
                    }
                    else
                    {
                        slashed[entry.UnbondeeAddress] = slashedSingle;
                    }
                }

                updatedEntries = Entries.SetItem(expireHeight, slashedEntries);
            }

            foreach (var (address, slashedEach) in slashed)
            {
                var delegatee = Repository!.GetDelegatee(address);
                var delegator = Repository!.GetDelegator(DelegatorAddress);
                delegatee.Unbond(delegator, delegatee.ShareFromFAV(slashedEach), height);

                var delegationBalance = Repository!.GetBalance(delegatee.DelegationPoolAddress, slashedEach.Currency);
                var slashAmount = slashedEach;
                if (delegationBalance < slashedEach)
                {
                    slashAmount = delegationBalance;
                }

                if (slashAmount > slashedEach.Currency * 0)
                {
                    Repository.TransferAsset(delegatee.DelegationPoolAddress, slashedPoolAddress, slashAmount);
                }              
            }


            return UpdateEntries(updatedEntries);
        }

        IUnbonding IUnbonding.Slash(
            BigInteger slashFactor,
            long infractionHeight,
            long height,
            Address slashedPoolAddress)
            => Slash(slashFactor, infractionHeight, height, slashedPoolAddress);

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
                new UnbondingEntry(
                    rebondeeAddress, initialGraceFAV, creationHeight, expireHeight));
        }

        private RebondGrace AddEntry(UnbondingEntry entry)
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
                    entry.ExpireHeight, ImmutableList<UnbondingEntry>.Empty.Add(entry)));
        }

        private RebondGrace UpdateEntries(
            ImmutableSortedDictionary<long, ImmutableList<UnbondingEntry>> entries)
            => new RebondGrace(Address, MaxEntries, DelegateeAddress, DelegatorAddress, entries, _repository);

        private void CannotMutateRelationsWithoutRepository()
        {
            if (_repository is null)
            {
                throw new InvalidOperationException(
                    "Cannot mutate without repository.");
            }
        }
    }
}
