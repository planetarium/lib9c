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
            BigInteger totalShares,
            IEnumerable<Currency> currencies,
            long? startHeight = null)
            : this(
                  address,
                  totalShares,
                  currencies.Select(c => c * 0),
                  RecommendedSigFig(totalShares),
                  startHeight)
        {
        }

        public RewardBase(
            Address address,
            BigInteger totalShares,
            IEnumerable<Currency> currencies,
            int sigFig,
            long? startHeight = null)
            : this(
                  address,
                  totalShares,
                  currencies.Select(c => c * 0),
                  sigFig,
                  startHeight)
        {
        }

        public RewardBase(Address address, IValue bencoded)
            : this(address, (List)bencoded)
        {
        }

        public RewardBase(
            Address address,
            BigInteger totalShares,
            IEnumerable<FungibleAssetValue> rewardPortion,
            int sigfig,
            long? startHeight = null)
        {
            Address = address;

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
            StartHeight = startHeight;
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
            TotalShares = (Integer)bencoded[2];
            var rewardPortion = ((List)bencoded[3]).Select(v => new FungibleAssetValue(v));

            if (!rewardPortion.Select(f => f.Currency).All(new HashSet<Currency>().Add))
            {
                throw new ArgumentException("Duplicated currency in reward base.");
            }

            RewardPortion = rewardPortion.ToImmutableDictionary(f => f.Currency, f => f);
            SigFig = (Integer)bencoded[4];

            try
            {
                StartHeight = (Integer)bencoded[5];
            }
            catch (IndexOutOfRangeException)
            {
                StartHeight = null;
            }
        }

        private RewardBase(
            Address address,
            BigInteger totalShares,
            ImmutableDictionary<Currency, FungibleAssetValue> rewardPortion,
            int sigfig,
            long? startHeight = null)
        {
            Address = address;
            TotalShares = totalShares;
            RewardPortion = rewardPortion;
            SigFig = sigfig;
            StartHeight = startHeight;
        }

        public Address Address { get; }

        public long? StartHeight { get; }

        public BigInteger TotalShares { get; }

        public int SigFig { get; private set; }

        public static int Margin => 2;

        public ImmutableDictionary<Currency, FungibleAssetValue> RewardPortion { get; }

        public List Bencoded
        {
            get
            {
                var bencoded = List.Empty
                    .Add(StateTypeName)
                    .Add(StateVersion)
                    .Add(TotalShares)
                    .Add(new List(RewardPortion
                        .OrderBy(r => r.Key, _currencyComparer)
                        .Select(r => r.Value.Serialize())))
                    .Add(SigFig);

                return StartHeight is long height
                    ? bencoded.Add(height)
                    : bencoded;
            }   
        }

        IValue IBencodable.Bencoded => Bencoded;

        public RewardBase AddRewards(IEnumerable<FungibleAssetValue> rewards)
            => rewards.Aggregate(this, (accum, next) => AddReward(accum, next));

        public RewardBase AddReward(FungibleAssetValue reward)
            => AddReward(this, reward);

        public RewardBase UpdateTotalShares(BigInteger totalShares)
            => UpdateTotalShares(this, totalShares);

        public RewardBase AttachHeight(Address address, long startHeight)
            => StartHeight is null
                ? new RewardBase(
                    address,
                    TotalShares,
                    RewardPortion,
                    SigFig,
                    startHeight)
                : throw new InvalidOperationException("StartHeight is already attached.");

        private static RewardBase AddReward(RewardBase rewardBase, FungibleAssetValue reward)
            => new RewardBase(
                rewardBase.Address,
                rewardBase.TotalShares,
                rewardBase.RewardPortion.TryGetValue(reward.Currency, out var portion)
                    ? rewardBase.RewardPortion.SetItem(
                        reward.Currency,
                        portion + (reward * Multiplier(rewardBase.SigFig)).DivRem(rewardBase.TotalShares).Quotient)
                    : throw new ArgumentException($"Invalid reward currency: {reward.Currency}"),
                rewardBase.SigFig,
                rewardBase.StartHeight);

        private static RewardBase UpdateTotalShares(RewardBase rewardBase, BigInteger totalShares)
        {
            var newSigFig = Math.Max(rewardBase.SigFig, RecommendedSigFig(totalShares));
            var multiplier = Multiplier(newSigFig - rewardBase.SigFig);
            var newPortion = rewardBase.RewardPortion.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value * multiplier);

            return new RewardBase(
                rewardBase.Address,
                totalShares,
                newPortion,
                newSigFig);
        }

        public static int RecommendedSigFig(BigInteger totalShares)
            => (int)Math.Floor(BigInteger.Log10(totalShares)) + Margin;

        public static BigInteger Multiplier(int sigFig)
            => BigInteger.Pow(10, sigFig);

        public ImmutableSortedDictionary<Currency, FungibleAssetValue> CumulativeRewardDuringPeriod(BigInteger share)
            => RewardPortion.Keys.Select(k => CumulativeRewardDuringPeriod(share, k))
                .ToImmutableSortedDictionary(f => f.Currency, f => f, _currencyComparer);

        public FungibleAssetValue CumulativeRewardDuringPeriod(BigInteger share, Currency currency)
            => RewardPortion.TryGetValue(currency, out var portion)
                ? (portion * share).DivRem(Multiplier(SigFig)).Quotient
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
