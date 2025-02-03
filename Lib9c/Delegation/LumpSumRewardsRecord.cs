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
using Nekoyume.Action;

namespace Nekoyume.Delegation
{
    public class LumpSumRewardsRecord : IBencodable, IEquatable<LumpSumRewardsRecord>
    {
        private const string StateTypeName = "lump_sum_rewards_record";
        private const long StateVersion = 1;

        public LumpSumRewardsRecord(
            Address address,
            long startHeight,
            BigInteger totalShares,
            IEnumerable<Currency> currencies)
            : this(
                  address,
                  startHeight,
                  totalShares,
                  currencies,
                  null)
        {
        }

        public LumpSumRewardsRecord(
            Address address,
            long startHeight,
            BigInteger totalShares,
            IEnumerable<Currency> currencies,
            long? lastStartHeight)
            : this(
                  address,
                  startHeight,
                  totalShares,
                  currencies.Select(c => c * 0),
                  lastStartHeight)
        {
        }

        public LumpSumRewardsRecord(
            Address address,
            long startHeight,
            BigInteger totalShares,
            IEnumerable<FungibleAssetValue> lumpSumRewards,
            long? lastStartHeight)
        {
            Address = address;
            StartHeight = startHeight;
            TotalShares = totalShares;

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
        {
            long startHeight;
            BigInteger totalShares;
            IEnumerable<FungibleAssetValue> lumpSumRewards;
            long? lastStartHeight;

            // TODO: Remove this if block after migration to state version 1 is done.
            if (bencoded[0] is not Text)
            {
                // Assume state version 0
                startHeight = (Integer)bencoded[0];
                totalShares = (Integer)bencoded[1];
                lumpSumRewards = ((List)bencoded[3]).Select(v => new FungibleAssetValue(v));
                lastStartHeight = (Integer?)bencoded.ElementAtOrDefault(4);
            }
            else
            {
                if (bencoded[0] is not Text text || text != StateTypeName || bencoded[1] is not Integer integer)
                {
                    throw new InvalidCastException();
                }

                if (integer > StateVersion)
                {
                    throw new FailedLoadStateException("Un-deserializable state.");
                }

                startHeight = (Integer)bencoded[2];
                totalShares = (Integer)bencoded[3];
                lumpSumRewards = ((List)bencoded[4]).Select(v => new FungibleAssetValue(v));
                lastStartHeight = (Integer?)bencoded.ElementAtOrDefault(5);
            }

            Address = address;
            StartHeight = startHeight;
            TotalShares = totalShares;

            if (!lumpSumRewards.Select(f => f.Currency).All(new HashSet<Currency>().Add))
            {
                throw new ArgumentException("Duplicated currency in lump sum rewards.");
            }

            LumpSumRewards = lumpSumRewards.ToImmutableDictionary(f => f.Currency, f => f);
            LastStartHeight = lastStartHeight;
        }

        private LumpSumRewardsRecord(
            Address address,
            long startHeight,
            BigInteger totalShares,
            ImmutableDictionary<Currency, FungibleAssetValue> lumpSumRewards,
            long? lastStartHeight)
        {
            Address = address;
            StartHeight = startHeight;
            TotalShares = totalShares;
            LumpSumRewards = lumpSumRewards;
            LastStartHeight = lastStartHeight;
        }

        public Address Address { get; }

        public long StartHeight { get; }

        public BigInteger TotalShares { get; }

        public ImmutableDictionary<Currency, FungibleAssetValue> LumpSumRewards { get; }

        public long? LastStartHeight { get; }

        public List Bencoded
        {
            get
            {
                var bencoded = List.Empty
                    .Add(StateTypeName)
                    .Add(StateVersion)
                    .Add(StartHeight)
                    .Add(TotalShares)
                    .Add(new List(LumpSumRewards
                        .OrderBy(r => r.Key, CurrencyComparer.HashBytes)
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
                record.LumpSumRewards.TryGetValue(rewards.Currency, out var cumulative)
                    ? record.LumpSumRewards.SetItem(rewards.Currency, cumulative + rewards)
                    : throw new ArgumentException($"Invalid reward currency: {rewards.Currency}"),
                record.LastStartHeight);

        public ImmutableSortedDictionary<Currency, FungibleAssetValue> RewardsDuringPeriod(BigInteger share)
            => LumpSumRewards.Keys.Select(k => RewardsDuringPeriod(share, k))
                .ToImmutableSortedDictionary(f => f.Currency, f => f, CurrencyComparer.HashBytes);

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
            && LastStartHeight == lumpSumRewardRecord.LastStartHeight);

        public override int GetHashCode()
            => Address.GetHashCode();
    }
}
