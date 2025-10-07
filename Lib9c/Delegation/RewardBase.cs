#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Lib9c.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Delegation
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

        /// <summary>
        /// Margin for significant figure. It's used to calculate the significant figure of the reward base.
        /// </summary>
        public const int Margin = 2;

        /// <summary>
        /// Constructor for new <see cref="RewardBase"/>.
        /// This constructor is used only for the initial reward base creation.
        /// </summary>
        /// <param name="address">
        /// <see cref="Address"> of <see cref="RewardBase"/>.
        /// </param>
        /// <param name="totalShares">
        /// <see cref="IDelegatee.TotalShares"/> of <see cref="RewardBase"/>'s creation height.
        /// </param>
        /// <param name="currencies">
        /// <see cref="IDelegatee.RewardCurrencies"/> of <see cref="RewardBase"/>'s creation height.
        /// It initializes the reward portion with 0.
        /// </param>
        public RewardBase(
            Address address,
            BigInteger totalShares,
            IEnumerable<Currency> currencies)
            : this(
                  address,
                  currencies.Select(c => (c, BigInteger.Zero)),
                  RecommendedSigFig(totalShares),
                  null)
        {
        }

        /// <summary>
        /// Constructor for new <see cref="RewardBase"/> from bencoded data.
        /// </summary>
        /// <param name="address">
        /// <see cref="Address"> of <see cref="RewardBase"/>.
        /// </param>
        /// <param name="bencoded">
        /// Bencoded data of <see cref="RewardBase"/>.
        /// </param>
        public RewardBase(Address address, IValue bencoded)
            : this(address, (List)bencoded)
        {
        }

        /// <summary>
        /// Constructor for new <see cref="RewardBase"/> from bencoded data.
        /// </summary>
        /// <param name="address">
        /// <see cref="Address"> of <see cref="RewardBase"/>.
        /// </param>
        /// <param name="bencoded">
        /// Bencoded data of <see cref="RewardBase"/>.
        /// <exception cref="InvalidCastException">
        /// Thrown when the bencoded data is not valid format for <see cref="RewardBase"/>.
        /// </exception>
        /// <exception cref="FailedLoadStateException">
        /// Thrown when the <see cref="StateVersion"/> of the bencoded data is higher than the current version.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the bencoded data has duplicated currency.
        /// </exception>
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
            var bencodedRewardPortion = ((List)bencoded[2]).Select(v => (List)v);
            var rewardPortion = bencodedRewardPortion.Select(
                p => (Currency: new Currency(p[0]), Portion: (BigInteger)(Integer)p[1]));

            if (!rewardPortion.Select(f => f.Currency).All(new HashSet<Currency>().Add))
            {
                throw new ArgumentException("Duplicated currency in reward base.");
            }

            RewardPortion = rewardPortion.ToImmutableSortedDictionary(f => f.Currency, f => f.Portion, CurrencyComparer.HashBytes);
            SigFig = (Integer)bencoded[3];

            try
            {
                StartHeight = (Integer)bencoded[4];
            }
            catch (IndexOutOfRangeException)
            {
                StartHeight = null;
            }
        }

        /// <summary>
        /// Constructor for new <see cref="RewardBase"/>.
        /// This constructor is used only for the initial reward base creation.
        /// </summary>
        /// <param name="address">
        /// <see cref="Address"> of <see cref="RewardBase"/>.
        /// </param>
        /// <param name="currencies">
        /// <see cref="IDelegatee.RewardCurrencies"/> of <see cref="RewardBase"/>'s creation height.
        /// It initializes the reward portion with 0.
        /// <param name="sigFig">
        /// Significant figure of <see cref="RewardBase"/>.
        /// </param>
        private RewardBase(
            Address address,
            IEnumerable<Currency> currencies,
            int sigFig)
            : this(
                  address,
                  currencies.Select(c => (c, BigInteger.Zero)),
                  sigFig,
                  null)
        {
        }

        /// <summary>
        /// Constructor for new <see cref="RewardBase"/>.
        /// </summary>
        /// <param name="address">
        /// <see cref="Address"> of <see cref="RewardBase"/>.
        /// </param>
        /// <param name="rewardPortion">
        /// Cumulative reward portion of <see cref="RewardBase"/>'s creation height.
        /// </param>
        /// <param name="sigFig">
        /// Significant figure of <see cref="RewardBase"/>.
        /// </param>
        /// <param name="startHeight">
        /// Start height of <see cref="RewardBase"/> that attached when archived.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the <paramref name="totalShares"/> is less than or equal to 0.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the <paramref name="rewardPortion"/> has duplicated currency.
        /// </exception>
        private RewardBase(
            Address address,
            IEnumerable<(Currency, BigInteger)> rewardPortion,
            int sigFig,
            long? startHeight = null)
        {
            Address = address;

            if (!rewardPortion.Select(f => f.Item1).All(new HashSet<Currency>().Add))
            {
                throw new ArgumentException("Duplicated currency in reward base.");
            }

            RewardPortion = rewardPortion.ToImmutableSortedDictionary(f => f.Item1, f => f.Item2, CurrencyComparer.HashBytes);
            SigFig = sigFig;
            StartHeight = startHeight;
        }

        /// <summary>
        /// Constructor for new <see cref="RewardBase"/>.
        /// </summary>
        /// <param name="address">
        /// <see cref="Address"> of <see cref="RewardBase"/>.
        /// </param>
        /// <param name="rewardPortion">
        /// Cumulative reward portion of <see cref="RewardBase"/>'s creation height.
        /// </param>
        /// <param name="sigFig">
        /// Significant figure of <see cref="RewardBase"/>.
        /// </param>
        /// <param name="startHeight">
        /// Start height of <see cref="RewardBase"/> that attached when archived.
        /// </param>
        private RewardBase(
            Address address,
            ImmutableSortedDictionary<Currency, BigInteger> rewardPortion,
            int sigFig,
            long? startHeight = null)
        {
            Address = address;
            RewardPortion = rewardPortion;
            SigFig = sigFig;
            StartHeight = startHeight;
        }

        /// <summary>
        /// <see cref="Address"> of <see cref="RewardBase"/>.
        /// </summary>
        public Address Address { get; }

        /// <summary>
        /// Cumulative reward portion of <see cref="RewardBase"/>.
        /// When it's multiplied by the number of shares, it will be the reward for the period.
        /// </summary>
        public ImmutableSortedDictionary<Currency, BigInteger> RewardPortion { get; }

        /// <summary>
        /// Significant figure of <see cref="RewardBase"/>.
        /// </summary>
        public int SigFig { get; private set; }

        /// <summary>
        /// Start height of <see cref="RewardBase"/> that attached when archived.
        /// </summary>
        public long? StartHeight { get; }

        public List Bencoded
        {
            get
            {
                var bencoded = List.Empty
                    .Add(StateTypeName)
                    .Add(StateVersion)
                    .Add(new List(RewardPortion
                        .OrderBy(r => r.Key, CurrencyComparer.HashBytes)
                        .Select(r => new List(r.Key.Serialize(), new Integer(r.Value)))))
                    .Add(SigFig);

                return StartHeight is long height
                    ? bencoded.Add(height)
                    : bencoded;
            }
        }

        IValue IBencodable.Bencoded => Bencoded;

        /// <summary>
        /// Add rewards to the <see cref="RewardBase"/>.
        /// </summary>
        /// <param name="rewards">
        /// Rewards to add.
        /// </param>
        /// <param name="totalShares">
        /// <see cref="IDelegatee.TotalShares"/> used as denominator of the portion.</param>
        /// <returns>
        /// New <see cref="RewardBase"/> with added rewards.
        /// </returns>
        public RewardBase AddRewards(IEnumerable<FungibleAssetValue> rewards, BigInteger totalShares)
            => rewards.Aggregate(this, (accum, next) => AddReward(accum, next, totalShares));

        /// <summary>
        /// Add reward to the <see cref="RewardBase"/>.
        /// </summary>
        /// <param name="reward">
        /// Reward to add.
        /// </param>
        /// <param name="totalShares">
        /// <see cref="IDelegatee.TotalShares"/> used as denominator of the portion.</param>
        /// <returns>
        /// <returns>
        /// New <see cref="RewardBase"/> with added reward.
        /// </returns>
        public RewardBase AddReward(FungibleAssetValue reward, BigInteger totalShares)
            => AddReward(this, reward, totalShares);

        /// <summary>
        /// Update the total shares of the <see cref="RewardBase"/>.
        /// </summary>
        /// <param name="totalShares">
        /// <see cref="IDelegatee.TotalShares"/> used as denominator of the portion.</param>
        /// </param>
        /// <returns>
        /// New <see cref="RewardBase"/> with updated total shares.
        /// </returns>
        public RewardBase UpdateSigFig(BigInteger totalShares)
            => UpdateSigFig(this, totalShares);

        /// <summary>
        /// Attach the start height to the <see cref="RewardBase"/> to be archived.
        /// </summary>
        /// <param name="address">
        /// <see cref="Address"> of <see cref="RewardBase"/>.
        /// </param>
        /// <param name="startHeight">
        /// Start height of <see cref="RewardBase"/> that attached when archived.
        /// </param>
        /// <returns>
        /// New <see cref="RewardBase"/> with attached start height.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the start height is already attached.
        /// </exception>
        public RewardBase AttachHeight(Address address, long startHeight)
            => StartHeight is null
                ? new RewardBase(
                    address,
                    RewardPortion,
                    SigFig,
                    startHeight)
                : throw new InvalidOperationException("StartHeight is already attached.");

        /// <summary>
        /// Calculate the cumulative reward during the period.
        /// </summary>
        /// <param name="share">
        /// The number of shares to calculate the reward.
        /// </param>
        /// <returns>
        /// Cumulative reward during the period.
        /// </returns>
        public ImmutableSortedDictionary<Currency, FungibleAssetValue> CumulativeRewardDuringPeriod(BigInteger share)
            => RewardPortion.Keys.Select(k => CumulativeRewardDuringPeriod(share, k))
                .ToImmutableSortedDictionary(f => f.Currency, f => f, CurrencyComparer.HashBytes);

        /// <summary>
        /// Calculate the cumulative reward during the period, for the specific currency.
        /// </summary>
        /// <param name="share">
        /// The number of shares to calculate the reward.
        /// </param>
        /// <param name="currency">
        /// The currency to calculate the reward.
        /// </param>
        /// <returns>
        /// Cumulative reward during the period, for the specific currency.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the <paramref name="currency"/> is not in the <see cref="RewardPortion"/>.
        /// </exception>
        public FungibleAssetValue CumulativeRewardDuringPeriod(BigInteger share, Currency currency)
            => RewardPortion.TryGetValue(currency, out var portion)
                ? FungibleAssetValue.FromRawValue(currency, (portion * share) / (Multiplier(SigFig)))
                : throw new ArgumentException($"Invalid reward currency: {currency}");

        private static RewardBase AddReward(RewardBase rewardBase, FungibleAssetValue reward, BigInteger totalShares)
        {
            if (!rewardBase.RewardPortion.TryGetValue(reward.Currency, out var portion))
            {
                throw new ArgumentException(
                    $"Invalid reward currency: {reward.Currency}", nameof(reward));
            }

            var portionNumerator = reward.RawValue * Multiplier(rewardBase.SigFig);
            var updatedPortion = portion + (portionNumerator / totalShares);

            return new RewardBase(
                rewardBase.Address,
                rewardBase.RewardPortion.SetItem(reward.Currency, updatedPortion),
                rewardBase.SigFig,
                rewardBase.StartHeight);
        }

        private static RewardBase UpdateSigFig(RewardBase rewardBase, BigInteger totalShares)
        {
            var newSigFig = Math.Max(rewardBase.SigFig, RecommendedSigFig(totalShares));
            var multiplier = Multiplier(newSigFig - rewardBase.SigFig);
            var newPortion = rewardBase.RewardPortion.ToImmutableSortedDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value * multiplier,
                CurrencyComparer.HashBytes);

            return new RewardBase(
                rewardBase.Address,
                newPortion,
                newSigFig);
        }

        public static int RecommendedSigFig(BigInteger totalShares)
            => (int)Math.Floor(BigInteger.Log10(totalShares)) + Margin;

        private static BigInteger Multiplier(int sigFig)
            => BigInteger.Pow(10, sigFig);

        public override bool Equals(object? obj)
            => obj is RewardBase other && Equals(other);

        public bool Equals(RewardBase? other)
            => ReferenceEquals(this, other)
            || (other is RewardBase rewardBase
            && Address == rewardBase.Address
            && RewardPortion.SequenceEqual(rewardBase.RewardPortion)
            && SigFig == rewardBase.SigFig
            && StartHeight == rewardBase.StartHeight);

        public override int GetHashCode()
            => Address.GetHashCode();
    }
}
