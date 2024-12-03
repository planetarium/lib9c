#nullable enable
namespace Lib9c.Tests.Delegation.Migration
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Numerics;
    using Bencodex;
    using Bencodex.Types;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Delegation;

    public class LegacyLumpSumRewardsRecord : IBencodable
    {
        private readonly IComparer<Currency> _currencyComparer = new CurrencyComparer();

        public LegacyLumpSumRewardsRecord(
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

        public LegacyLumpSumRewardsRecord(
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

        public LegacyLumpSumRewardsRecord(
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

        public LegacyLumpSumRewardsRecord(Address address, IValue bencoded)
            : this(address, (List)bencoded)
        {
        }

        public LegacyLumpSumRewardsRecord(Address address, List bencoded)
            : this(
                address,
                (Integer)bencoded[0],
                (Integer)bencoded[1],
                ((List)bencoded[2]).Select(a => new Address(a)).ToImmutableSortedSet(),
                ((List)bencoded[3]).Select(v => new FungibleAssetValue(v)),
                (Integer?)bencoded.ElementAtOrDefault(4))
        {
        }

        private LegacyLumpSumRewardsRecord(
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
    }
}
