#nullable enable
using System;
using System.Numerics;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.TableData;
using Nekoyume.TableData.Swap;

namespace Nekoyume.Model.Swap
{
    /// <summary>
    /// Swap pool for swap between currencies.
    /// </summary>
    public class SwapPool
    {
        /// <summary>
        /// Constructor for <see cref="SwapPool"/>.
        /// </summary>
        /// <param name="swapRateSheet">
        /// Swap rate sheet to refer.
        /// </param>
        public SwapPool(SwapRateSheet swapRateSheet)
        {
            SwapRateSheet = swapRateSheet;
        }

        /// <summary>
        /// Swap rate sheet to refer.
        /// </summary>
        public SwapRateSheet SwapRateSheet { get; }

        /// <summary>
        /// Address of the swap pool.
        /// </summary>
        public Address Address => Addresses.SwapPool;

        /// <summary>
        /// Swap the currency from <paramref name="from"/> to <paramref name="to"/>.
        /// </summary>
        /// <param name="world">
        /// <see cref="IWorld"/> to swap the currency.
        /// </param>
        /// <param name="context">
        /// <see cref="IActionContext"/> to swap the currency.
        /// </param>
        /// <param name="from">
        /// <see cref="FungibleAssetValue"/> to swap from.
        /// </param>
        /// <param name="to">
        /// <see cref="Currency"/> to swap to.
        /// </param>
        /// <returns>
        /// New <see cref="IWorld"/> after swapping the currency.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="from"/> and <paramref name="to"/> are the same currency.
        /// </exception>
        public IWorld Swap(IWorld world, IActionContext context, FungibleAssetValue from, Currency to)
        {
            if (from.Currency.Equals(to))
            {
                throw new ArgumentException("Cannot swap the same currency.", nameof(to));
            }
        
            var swapFAV = convertToSwapFAV(from, to, out var remainder);
            var newWorld = world
                .TransferAsset(context, context.Signer, Address, from - remainder)
                .TransferAsset(context, Address, context.Signer, swapFAV);
            return newWorld;
        }

        /// <summary>
        /// Convert <paramref name="from"/> to <paramref name="to"/> with swap rate.
        /// </summary>
        /// <param name="from">
        /// <see cref="FungibleAssetValue"/> to swap from.
        /// </param>
        /// <param name="to">
        /// <see cref="Currency"/> to swap to.
        /// </param>
        /// <param name="remainder">
        /// Remainder of the swap. This will be returned to the <see cref="Address"/> who requested swap.
        /// </param>
        /// <returns>
        /// <see cref="FungibleAssetValue"/> after swapping.
        /// </returns>
        /// <exception cref="SheetRowNotFoundException">
        /// Thrown when the swap rate sheet does not contain the rate for <paramref name="from"/> and <paramref name="to"/>.
        /// </exception>
        public FungibleAssetValue convertToSwapFAV(
            FungibleAssetValue from,
            Currency to,
            out FungibleAssetValue remainder)
        {
            if (from.Currency.Equals(to))
            {
                remainder = FungibleAssetValue.FromRawValue(from.Currency, 0);
                return from;
            }

            if (!SwapRateSheet.TryGetValue(new SwapRateSheet.CurrencyPair(from.Currency, to), out var row))
            {
                throw new SheetRowNotFoundException(nameof(SwapRateSheet), string.Join(from.Currency.ToString(), to.ToString()));
            }

            return convertToSwapFAV(from, to, row.Rate.Numerator, row.Rate.Denominator, out remainder);
        }

        /// <summary>
        /// Convert <paramref name="from"/> to <paramref name="to"/> with swap rate.
        /// </summary>
        /// <param name="from">
        /// <see cref="FungibleAssetValue"/> to swap from.
        /// </param>
        /// <param name="to">
        /// <see cref="Currency"/> to swap to.
        /// </param>
        /// <param name="rateNumerator">
        /// Numerator of the swap rate.
        /// </param>
        /// <param name="rateDenominator">
        /// Denominator of the swap rate.
        /// </param>
        /// <param name="remainder">
        /// Remainder of the swap. This will be returned to the <see cref="Address"/> who requested swap.
        /// </param>
        /// <returns>
        /// <see cref="FungibleAssetValue"/> after swapping.
        /// </returns>
        public static FungibleAssetValue convertToSwapFAV(
            FungibleAssetValue from,
            Currency to,
            BigInteger rateNumerator,
            BigInteger rateDenominator,
            out FungibleAssetValue remainder)
        {
            var fromCurrency = from.Currency;
            var decimalDiff = to.DecimalPlaces - fromCurrency.DecimalPlaces;

            if (decimalDiff < 0)
            {
                var downscaleFactor = BigInteger.Pow(10, -decimalDiff);
                var rateAppliedRawValue = BigInteger.DivRem(
                    from.RawValue * rateNumerator,
                    rateDenominator,
                    out var rateRemainderRawValue);
                var scaledRateAppliedRawValue = BigInteger.DivRem(
                    rateAppliedRawValue,
                    downscaleFactor,
                    out var scaleRemainderRawValue);
                var value = FungibleAssetValue.FromRawValue(to, scaledRateAppliedRawValue);

                var remainderRawValue = (scaleRemainderRawValue * downscaleFactor) + rateRemainderRawValue;
                remainder = FungibleAssetValue.FromRawValue(fromCurrency, remainderRawValue);
                return value;
            }
            else
            {
                var upscaleFactor = BigInteger.Pow(10, decimalDiff);
                var rateAppliedRawValue = BigInteger.DivRem(
                    from.RawValue * rateNumerator * upscaleFactor,
                    rateDenominator,
                    out var scaledRemainderRawValue);
                var value = FungibleAssetValue.FromRawValue(to, rateAppliedRawValue);
                var remainderRawValue = BigInteger.DivRem(scaledRemainderRawValue, upscaleFactor, out _);
                remainder = FungibleAssetValue.FromRawValue(fromCurrency, remainderRawValue);
                return value;
            }
        }
    }
}
