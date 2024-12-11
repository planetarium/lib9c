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

        /// <summary>
        /// Margin for significant figure. It's used to calculate the significant figure of the reward base.
        /// </summary>
        public const int Margin = 2;
        private static readonly IComparer<Currency> _currencyComparer = new CurrencyComparer();

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
                  totalShares,
                  currencies.Select(c => c * 0),
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
            TotalShares = (Integer)bencoded[2];
            var rewardPortion = ((List)bencoded[3]).Select(v => new FungibleAssetValue(v));

            if (!rewardPortion.Select(f => f.Currency).All(new HashSet<Currency>().Add))
            {
                throw new ArgumentException("Duplicated currency in reward base.");
            }

            RewardPortion = rewardPortion.ToImmutableSortedDictionary(f => f.Currency, f => f, _currencyComparer);
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

        /// <summary>
        /// Constructor for new <see cref="RewardBase"/>.
        /// </summary>
        /// <param name="address">
        /// <see cref="Address"> of <see cref="RewardBase"/>.
        /// </param>
        /// <param name="totalShares">
        /// <see cref="IDelegatee.TotalShares"/> of <see cref="RewardBase"/>'s creation height.
        /// </param>
        /// <param name="rewardPortion">
        /// Cumulative reward portion of <see cref="RewardBase"/>'s creation height.
        /// </param>
        /// <param name="sigfig">
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

            RewardPortion = rewardPortion.ToImmutableSortedDictionary(f => f.Currency, f => f, _currencyComparer);
            SigFig = sigfig;
            StartHeight = startHeight;
        }

        /// <summary>
        /// Constructor for new <see cref="RewardBase"/>.
        /// </summary>
        /// <param name="address">
        /// <see cref="Address"> of <see cref="RewardBase"/>.
        /// </param>
        /// <param name="totalShares">
        /// <see cref="IDelegatee.TotalShares"/> of <see cref="RewardBase"/>'s creation height.
        /// </param>
        /// <param name="rewardPortion">
        /// Cumulative reward portion of <see cref="RewardBase"/>'s creation height.
        /// </param>
        /// <param name="sigfig">
        /// Significant figure of <see cref="RewardBase"/>.
        /// </param>
        /// <param name="startHeight">
        /// Start height of <see cref="RewardBase"/> that attached when archived.
        /// </param>
        private RewardBase(
            Address address,
            BigInteger totalShares,
            ImmutableSortedDictionary<Currency, FungibleAssetValue> rewardPortion,
            int sigfig,
            long? startHeight = null)
        {
            Address = address;
            TotalShares = totalShares;
            RewardPortion = rewardPortion;
            SigFig = sigfig;
            StartHeight = startHeight;
        }

        /// <summary>
        /// <see cref="Address"> of <see cref="RewardBase"/>.
        /// </summary>
        public Address Address { get; }

        /// <summary>
        /// Start height of <see cref="RewardBase"/> that attached when archived.
        /// </summary>
        public long? StartHeight { get; }

        /// <summary>
        /// <see cref="IDelegatee.TotalShares"/> of <see cref="RewardBase"/>'s creation height.
        /// </summary>
        public BigInteger TotalShares { get; }

        /// <summary>
        /// Significant figure of <see cref="RewardBase"/>.
        /// </summary>
        public int SigFig { get; private set; }

        /// <summary>
        /// Cumulative reward portion of <see cref="RewardBase"/>.
        /// When it's multiplied by the number of shares, it will be the reward for the period.
        /// </summary>
        public ImmutableSortedDictionary<Currency, FungibleAssetValue> RewardPortion { get; }

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

        /// <summary>
        /// Add rewards to the <see cref="RewardBase"/>.
        /// </summary>
        /// <param name="rewards">
        /// Rewards to add.
        /// </param>
        /// <returns>
        /// New <see cref="RewardBase"/> with added rewards.
        /// </returns>
        public RewardBase AddRewards(IEnumerable<FungibleAssetValue> rewards)
            => rewards.Aggregate(this, (accum, next) => AddReward(accum, next));

        /// <summary>
        /// Add reward to the <see cref="RewardBase"/>.
        /// </summary>
        /// <param name="reward">
        /// Reward to add.
        /// </param>
        /// <returns>
        /// New <see cref="RewardBase"/> with added reward.
        /// </returns>
        public RewardBase AddReward(FungibleAssetValue reward)
            => AddReward(this, reward);

        /// <summary>
        /// Update the total shares of the <see cref="RewardBase"/>.
        /// </summary>
        /// <param name="totalShares">
        /// New <see cref="IDelegatee.TotalShares"/> of the height that <see cref="RewardBase"/> created.
        /// </param>
        /// <returns>
        /// New <see cref="RewardBase"/> with updated total shares.
        /// </returns>
        public RewardBase UpdateTotalShares(BigInteger totalShares)
            => UpdateTotalShares(this, totalShares);

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
                    TotalShares,
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
                .ToImmutableSortedDictionary(f => f.Currency, f => f, _currencyComparer);

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
                ? (portion * share).DivRem(Multiplier(SigFig)).Quotient
                : throw new ArgumentException($"Invalid reward currency: {currency}");

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
            var newPortion = rewardBase.RewardPortion.ToImmutableSortedDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value * multiplier,
                _currencyComparer);

            return new RewardBase(
                rewardBase.Address,
                totalShares,
                newPortion,
                newSigFig);
        }

        private static int RecommendedSigFig(BigInteger totalShares)
            => (int)Math.Floor(BigInteger.Log10(totalShares)) + Margin;

        private static BigInteger Multiplier(int sigFig)
            => BigInteger.Pow(10, sigFig);


        public override bool Equals(object? obj)
            => obj is RewardBase other && Equals(other);

        public bool Equals(RewardBase? other)
            => ReferenceEquals(this, other)
            || (other is RewardBase rewardBase
            && Address == rewardBase.Address
            && TotalShares == rewardBase.TotalShares
            && RewardPortion.SequenceEqual(rewardBase.RewardPortion)
            && SigFig == rewardBase.SigFig);

        public override int GetHashCode()
            => Address.GetHashCode();
    }
}
