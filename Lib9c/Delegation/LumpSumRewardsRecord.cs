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
            Currency currency,
            BigInteger totalShares)
            : this(address, totalShares, -1, currency * 0, null)
        {
        }

        public LumpSumRewardsRecord(
            Address address,
            Currency currency,
            BigInteger totalShares,
            long lastStartHeight)
            : this(address, totalShares, lastStartHeight, currency * 0, null)
        {
        }

        public LumpSumRewardsRecord(
            Address address,
            BigInteger totalShares,
            long lastStartHeight,
            FungibleAssetValue lumpSumRewards,
            long? startHeight)
        {
            Address = address;
            TotalShares = totalShares;
            LastStartHeight = lastStartHeight;
            LumpSumRewards = lumpSumRewards;
            StartHeight = startHeight;
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

        public BigInteger TotalShares { get; }

        public long LastStartHeight { get; }

        public FungibleAssetValue LumpSumRewards { get; }

        public long? StartHeight { get; }

        public bool IsStarted => StartHeight is long;

        public List Bencoded
        {
            get
            {
                var bencoded = List.Empty
                    .Add(TotalShares)
                    .Add(LastStartHeight)
                    .Add(LumpSumRewards.Serialize());

                return StartHeight is long startHeight
                    ? bencoded.Add(startHeight)
                    : bencoded;
            }
        }

        IValue IBencodable.Bencoded => Bencoded;

        public LumpSumRewardsRecord Start(long height)
            => new LumpSumRewardsRecord(
                Address, TotalShares, LastStartHeight, LumpSumRewards, height);

        public LumpSumRewardsRecord AddLumpSumReward(FungibleAssetValue reward)
            => new LumpSumRewardsRecord(
                Address,
                TotalShares,
                LastStartHeight,
                LumpSumRewards + reward,
                StartHeight);

        public FungibleAssetValue RewardsDuringPeriod(BigInteger share)
            => (LumpSumRewards * share).DivRem(TotalShares, out _);

        public override bool Equals(object? obj)
            => obj is LumpSumRewardsRecord other && Equals(other);

        public bool Equals(LumpSumRewardsRecord? other)
            => ReferenceEquals(this, other)
            || (other is LumpSumRewardsRecord lumpSumRewardRecord
            && Address == lumpSumRewardRecord.Address
            && TotalShares == lumpSumRewardRecord.TotalShares
            && StartHeight == lumpSumRewardRecord.StartHeight
            && LumpSumRewards.Equals(lumpSumRewardRecord.LumpSumRewards)
            && LastStartHeight == lumpSumRewardRecord.LastStartHeight);

        public override int GetHashCode()
            => Address.GetHashCode();
    }
}
