#nullable enable
using System;
using Bencodex.Types;
using Bencodex;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Action.DPoS.Model
{
    public class LumpSumRewardsRecord : IBencodable, IEquatable<LumpSumRewardsRecord>
    {
        public LumpSumRewardsRecord(
            Address address,
            Currency currency,
            FungibleAssetValue totalShares)
            : this(address, totalShares, -1, currency * 0, -1)
        {
        }

        public LumpSumRewardsRecord(
            Address address,
            Currency currency,
            FungibleAssetValue totalShares,
            long lastStartHeight)
            : this(address, totalShares, lastStartHeight, currency * 0, -1)
        {
        }

        public LumpSumRewardsRecord(
            Address address,
            FungibleAssetValue totalShares,
            long lastStartHeight,
            FungibleAssetValue lumpSumRewards,
            long startHeight)
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
                new FungibleAssetValue(bencoded[0]),
                (Integer)bencoded[1],
                new FungibleAssetValue(bencoded[2]),
                (Integer)bencoded[3])
        {
        }

        public Address Address { get; }

        public FungibleAssetValue TotalShares { get; }

        public long LastStartHeight { get; }

        public FungibleAssetValue LumpSumRewards { get; }

        public long StartHeight { get; }

        public bool IsStarted => StartHeight != -1;

        public List Bencoded
        {
            get
            {
                var bencoded = List.Empty
                    .Add(TotalShares.Serialize())
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

        public FungibleAssetValue RewardsDuringPeriod(FungibleAssetValue share)
            => (LumpSumRewards * share.RawValue).DivRem(TotalShares.RawValue, out _);

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
