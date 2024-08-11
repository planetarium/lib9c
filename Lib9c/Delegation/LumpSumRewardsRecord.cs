#nullable enable
using System;
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
        public LumpSumRewardsRecord(
            Address address,
            long startHeight,
            BigInteger totalShares,
            Currency currency)
            : this(address, startHeight, totalShares, currency, null)
        {
        }

        public LumpSumRewardsRecord(
            Address address,
            long startHeight,
            BigInteger totalShares,
            Currency currency,
            long? lastStartHeight)
            : this(address, startHeight, totalShares, currency * 0, lastStartHeight)
        {
        }

        public LumpSumRewardsRecord(
            Address address,
            long startHeight,
            BigInteger totalShares,
            FungibleAssetValue lumpSumRewards,
            long? lastStartHeight)
        {
            Address = address;
            StartHeight = startHeight;
            TotalShares = totalShares;
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
                new FungibleAssetValue(bencoded[2]),
                (Integer?)bencoded.ElementAtOrDefault(3))
        {
        }

        public Address Address { get; }

        public long StartHeight { get; }

        public BigInteger TotalShares { get; }

        public FungibleAssetValue LumpSumRewards { get; }

        public long? LastStartHeight { get; }

        public List Bencoded
        {
            get
            {
                var bencoded = List.Empty
                    .Add(StartHeight)
                    .Add(TotalShares)
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
                LumpSumRewards,
                LastStartHeight);

        public LumpSumRewardsRecord AddLumpSumReward(FungibleAssetValue reward)
            => new LumpSumRewardsRecord(
                Address,
                StartHeight,
                TotalShares,
                LumpSumRewards + reward,
                LastStartHeight);

        public FungibleAssetValue RewardsDuringPeriod(BigInteger share)
            => (LumpSumRewards * share).DivRem(TotalShares, out _);

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
