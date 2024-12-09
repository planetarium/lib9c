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
using Nekoyume.Action;

namespace Nekoyume.Delegation
{
    /// <summary>
    /// RewardBase is a class that represents the base of the reward.
    /// If it's multiplied by the number of shares, it will be the reward for the period.
    /// Also, it holds the significant figure to calculate the reward.
    /// </summary>
    public class RewardBase : IBencodable, IEquatable<RewardBase>
    {
        private const string StateTypeName = "reward_base";
        private const long StateVersion = 1;
        private readonly IComparer<Currency> _currencyComparer = new CurrencyComparer();

        public RewardBase(
            Address address,
            long startHeight,
            BigInteger totalShares,
            IEnumerable<Currency> currencies)
            : this(
                  address,
                  startHeight,
                  totalShares,
                  currencies.Select(c => c * 0),
                  RecommendedSigFig(totalShares))
        {
        }

        public RewardBase(
            Address address,
            long startHeight,
            BigInteger totalShares,
            IEnumerable<Currency> currencies,
            int sigFig)
            : this(
                  address,
                  startHeight,
                  totalShares,
                  currencies.Select(c => c * 0),
                  sigFig)
        {
        }

        public RewardBase(Address address, IValue bencoded)
            : this(address, (List)bencoded)
        {
        }

        public RewardBase(
            Address address,
            long startHeight,
            BigInteger totalShares,
            IEnumerable<FungibleAssetValue> rewardPortion,
            int sigfig)
        {
            Address = address;
            StartHeight = startHeight;

            if (totalShares.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalShares));
            }

            TotalShares = totalShares;

            if (!rewardPortion.Select(f => f.Currency).All(new HashSet<Currency>().Add))
            {
                throw new ArgumentException("Duplicated currency in reward base.");
            }

            RewardPortion = rewardPortion.ToImmutableDictionary(f => f.Currency, f => f);
            SigFig = sigfig;
        }


        public RewardBase(Address address, List bencoded)
        {
            if (bencoded[0] is not Text text || text != StateTypeName || bencoded[1] is not Integer integer)
            {
                throw new InvalidCastException();
            }

            if (integer > StateVersion)
            {
                throw new FailedLoadStateException("Un-deserializable state.");
            }

            Address = address;
            StartHeight = (Integer)bencoded[2];
            TotalShares = (Integer)bencoded[3];
            var rewardPortion = ((List)bencoded[4]).Select(v => new FungibleAssetValue(v));

            if (!rewardPortion.Select(f => f.Currency).All(new HashSet<Currency>().Add))
            {
                throw new ArgumentException("Duplicated currency in reward base.");
            }

            RewardPortion = rewardPortion.ToImmutableDictionary(f => f.Currency, f => f);
            SigFig = (Integer)bencoded[5];
        }

        private RewardBase(
            Address address,
            long startHeight,
            BigInteger totalShares,
            ImmutableDictionary<Currency, FungibleAssetValue> rewardPortion,
            int sigfig)
        {
            Address = address;
            StartHeight = startHeight;
            TotalShares = totalShares;
            RewardPortion = rewardPortion;
            SigFig = sigfig;
        }

        public Address Address { get; }

        public long StartHeight { get; }

        public BigInteger TotalShares { get; }

        public int SigFig { get; private set; }

        public static int Margin => 2;

        public ImmutableDictionary<Currency, FungibleAssetValue> RewardPortion { get; }

        public List Bencoded
            => List.Empty
                .Add(StateTypeName)
                .Add(StateVersion)
                .Add(StartHeight)
                .Add(TotalShares)
                .Add(new List(RewardPortion
                    .OrderBy(r => r.Key, _currencyComparer)
                    .Select(r => r.Value.Serialize())))
                .Add(SigFig);

        IValue IBencodable.Bencoded => Bencoded;

        public RewardBase AddRewards(IEnumerable<FungibleAssetValue> rewards)
            => rewards.Aggregate(this, (accum, next) => AddReward(accum, next));

        public RewardBase AddReward(FungibleAssetValue reward)
            => AddReward(this, reward);

        public RewardBase UpdateTotalShares(BigInteger totalShares)
            => UpdateTotalShares(this, totalShares);

        public RewardBase MoveAddress(Address address)
            => new RewardBase(
                address,
                StartHeight,
                TotalShares,
                RewardPortion,
                SigFig);

        public static RewardBase AddReward(RewardBase rewardBase, FungibleAssetValue reward)
            => new RewardBase(
                rewardBase.Address,
                rewardBase.StartHeight,
                rewardBase.TotalShares,
                rewardBase.RewardPortion.TryGetValue(reward.Currency, out var portion)
                    ? rewardBase.RewardPortion.SetItem(
                        reward.Currency,
                        portion + (reward * rewardBase.SigFig).DivRem(rewardBase.TotalShares).Quotient)
                    : throw new ArgumentException($"Invalid reward currency: {reward.Currency}"),
                rewardBase.SigFig);

        public static RewardBase UpdateTotalShares(RewardBase rewardBase, BigInteger totalShares)
        {
            var newSigFig = Math.Max(rewardBase.SigFig, RecommendedSigFig(totalShares));
            var multiplier = BigInteger.Pow(10, newSigFig - rewardBase.SigFig);
            var newPortion = rewardBase.RewardPortion.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => (kvp.Value * multiplier).DivRem(rewardBase.TotalShares).Quotient);

            return new RewardBase(
                rewardBase.Address,
                rewardBase.StartHeight,
                totalShares,
                newPortion,
                newSigFig);
        }

        public static int RecommendedSigFig(BigInteger totalShares)
            => (int)Math.Floor(BigInteger.Log10(totalShares)) + Margin;

        public ImmutableSortedDictionary<Currency, FungibleAssetValue> CumulativeRewardDuringPeriod(BigInteger share)
            => RewardPortion.Keys.Select(k => CumulativeRewardDuringPeriod(share, k))
                .ToImmutableSortedDictionary(f => f.Currency, f => f, _currencyComparer);

        public FungibleAssetValue CumulativeRewardDuringPeriod(BigInteger share, Currency currency)
            => RewardPortion.TryGetValue(currency, out var portion)
                ? (portion * share).DivRem(SigFig).Quotient
                : throw new ArgumentException($"Invalid reward currency: {currency}");

        public override bool Equals(object? obj)
            => obj is RewardBase other && Equals(other);

        public bool Equals(RewardBase? other)
            => ReferenceEquals(this, other)
            || (other is RewardBase rewardBase
            && Address == rewardBase.Address
            && TotalShares == rewardBase.TotalShares
            && RewardPortion.Equals(rewardBase.RewardPortion)
            && SigFig == rewardBase.SigFig);

        public override int GetHashCode()
            => Address.GetHashCode();
    }
}
