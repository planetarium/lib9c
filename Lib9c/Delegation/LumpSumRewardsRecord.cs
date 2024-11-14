#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Bencodex;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public class LumpSumRewardsRecord : IBencodable, IEquatable<LumpSumRewardsRecord>
    {
        private readonly IComparer<Currency> _currencyComparer = new CurrencyComparer();

        public LumpSumRewardsRecord(
            Address address,
            long startHeight,
            BigInteger totalShares,
            ImmutableSortedSet<Address> delegators,
            IEnumerable<Currency> currencies)
            : this(
                  address,
                  startHeight,
                  totalShares,
                  delegators,
                  currencies,
                  null)
        {
        }

        public LumpSumRewardsRecord(
            Address address,
            long startHeight,
            BigInteger totalShares,
            ImmutableSortedSet<Address> delegators,
            IEnumerable<Currency> currencies,
            long? lastStartHeight)
            : this(
                  address,
                  startHeight,
                  totalShares,
                  delegators,
                  currencies.Select(c => c * 0),
                  lastStartHeight)
        {
        }

        public LumpSumRewardsRecord(
            Address address,
            long startHeight,
            BigInteger totalShares,
            ImmutableSortedSet<Address> delegators,
            IEnumerable<FungibleAssetValue> lumpSumRewards,
            long? lastStartHeight)
        {
            Address = address;
            StartHeight = startHeight;
            TotalShares = totalShares;
            Delegators = delegators;

            if (!lumpSumRewards.Select(f => f.Currency).All(new HashSet<Currency>().Add))
            {
                throw new ArgumentException("Duplicated currency in lump sum rewards.");
            }

            LumpSumRewards = lumpSumRewards.ToImmutableDictionary(f => f.Currency, f => f);
            LastStartHeight = lastStartHeight;
        }

        public LumpSumRewardsRecord(Address address, IValue bencoded)
            : this(address, (List)bencoded)
        {
        }

        public LumpSumRewardsRecord(Address address, List bencoded)
            : this(
                address,
                (Integer)bencoded[0],
                (Integer)bencoded[1],
                ((List)bencoded[2]).Select(a => new Address(a)).ToImmutableSortedSet(),
                ((List)bencoded[3]).Select(v => new FungibleAssetValue(v)),
                (Integer?)bencoded.ElementAtOrDefault(4))
        {
        }

        private LumpSumRewardsRecord(
            Address address,
            long startHeight,
            BigInteger totalShares,
            ImmutableSortedSet<Address> delegators,
            ImmutableDictionary<Currency, FungibleAssetValue> lumpSumRewards,
            long? lastStartHeight)
        {
            Address = address;
            StartHeight = startHeight;
            TotalShares = totalShares;
            Delegators = delegators;
            LumpSumRewards = lumpSumRewards;
            LastStartHeight = lastStartHeight;
        }

        public Address Address { get; }

        public long StartHeight { get; }

        public BigInteger TotalShares { get; }

        public ImmutableDictionary<Currency, FungibleAssetValue> LumpSumRewards { get; }

        public ImmutableSortedSet<Address> Delegators { get; }

        public long? LastStartHeight { get; }

        public List Bencoded
        {
            get
            {
                var bencoded = List.Empty
                    .Add(StartHeight)
                    .Add(TotalShares)
                    .Add(new List(Delegators.Select(a => a.Bencoded)))
                    .Add(new List(LumpSumRewards
                        .OrderBy(r => r.Key, _currencyComparer)
                        .Select(r => r.Value.Serialize())));

                return LastStartHeight is long lastStartHeight
                    ? bencoded.Add(lastStartHeight)
                    : bencoded;
            }
        }

        IValue IBencodable.Bencoded => Bencoded;

        public LumpSumRewardsRecord MoveAddress(Address address)
            => new LumpSumRewardsRecord(
                address,
                StartHeight,
                TotalShares,
                Delegators,
                LumpSumRewards,
                LastStartHeight);

        public LumpSumRewardsRecord AddLumpSumRewards(IEnumerable<FungibleAssetValue> rewards)
            => rewards.Aggregate(this, (accum, next) => AddLumpSumRewards(accum, next));

        public LumpSumRewardsRecord AddLumpSumRewards(FungibleAssetValue rewards)
            => AddLumpSumRewards(this, rewards);

        public static LumpSumRewardsRecord AddLumpSumRewards(LumpSumRewardsRecord record, FungibleAssetValue rewards)
            => new LumpSumRewardsRecord(
                record.Address,
                record.StartHeight,
                record.TotalShares,
                record.Delegators,
                record.LumpSumRewards.TryGetValue(rewards.Currency, out var cumulative)
                    ? record.LumpSumRewards.SetItem(rewards.Currency, cumulative + rewards)
                    : throw new ArgumentException($"Invalid reward currency: {rewards.Currency}"),
                record.LastStartHeight);

        public LumpSumRewardsRecord RemoveDelegator(Address delegator)
            => new LumpSumRewardsRecord(
                Address,
                StartHeight,
                TotalShares,
                Delegators.Remove(delegator),
                LumpSumRewards,
                LastStartHeight);

        public ImmutableSortedDictionary<Currency, FungibleAssetValue> RewardsDuringPeriod(BigInteger share)
            => LumpSumRewards.Keys.Select(k => RewardsDuringPeriod(share, k))
                .ToImmutableSortedDictionary(f => f.Currency, f => f, _currencyComparer);

        public FungibleAssetValue RewardsDuringPeriod(BigInteger share, Currency currency)
            => LumpSumRewards.TryGetValue(currency, out var reward)
                ? (reward * share).DivRem(TotalShares).Quotient
                : throw new ArgumentException($"Invalid reward currency: {currency}");


        public override bool Equals(object? obj)
            => obj is LumpSumRewardsRecord other && Equals(other);

        public bool Equals(LumpSumRewardsRecord? other)
            => ReferenceEquals(this, other)
            || (other is LumpSumRewardsRecord lumpSumRewardRecord
            && Address == lumpSumRewardRecord.Address
            && StartHeight == lumpSumRewardRecord.StartHeight
            && TotalShares == lumpSumRewardRecord.TotalShares
            && LumpSumRewards.Equals(lumpSumRewardRecord.LumpSumRewards)
            && LastStartHeight == lumpSumRewardRecord.LastStartHeight
            && Delegators.SequenceEqual(lumpSumRewardRecord.Delegators));

        public override int GetHashCode()
            => Address.GetHashCode();
    }
}
