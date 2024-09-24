#nullable enable
using System;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Bencodex;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using System.Collections.Immutable;

namespace Nekoyume.Delegation
{
    public class LumpSumRewardsRecord : IBencodable, IEquatable<LumpSumRewardsRecord>
    {
        public LumpSumRewardsRecord(
            Address address,
            long startHeight,
            BigInteger totalShares,
            ImmutableSortedSet<Address> delegators,
            Currency currency)
            : this(address, startHeight, totalShares, delegators, currency, null)
        {
        }

        public LumpSumRewardsRecord(
            Address address,
            long startHeight,
            BigInteger totalShares,
            ImmutableSortedSet<Address> delegators,
            Currency currency,
            long? lastStartHeight)
            : this(address, startHeight, totalShares, delegators, currency * 0, lastStartHeight)
        {
        }

        public LumpSumRewardsRecord(
            Address address,
            long startHeight,
            BigInteger totalShares,
            ImmutableSortedSet<Address> delegators,
            FungibleAssetValue lumpSumRewards,
            long? lastStartHeight)
        {
            Address = address;
            StartHeight = startHeight;
            TotalShares = totalShares;
            Delegators = delegators;
            LumpSumRewards = lumpSumRewards;
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
                new FungibleAssetValue(bencoded[3]),
                (Integer?)bencoded.ElementAtOrDefault(4))
        {
        }

        public Address Address { get; }

        public long StartHeight { get; }

        public BigInteger TotalShares { get; }

        public FungibleAssetValue LumpSumRewards { get; }

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
                    .Add(LumpSumRewards.Serialize());

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

        public LumpSumRewardsRecord AddLumpSumRewards(FungibleAssetValue rewards)
            => new LumpSumRewardsRecord(
                Address,
                StartHeight,
                TotalShares,
                Delegators,
                LumpSumRewards + rewards,
                LastStartHeight);

        public LumpSumRewardsRecord RemoveDelegator(Address delegator)
            => new LumpSumRewardsRecord(
                Address,
                StartHeight,
                TotalShares,
                Delegators.Remove(delegator),
                LumpSumRewards,
                LastStartHeight);

        public FungibleAssetValue RewardsDuringPeriod(BigInteger share)
            => (LumpSumRewards * share).DivRem(TotalShares).Quotient;

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
