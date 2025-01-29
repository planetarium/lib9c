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
    public class SwapPool
    {
        public SwapPool(SwapRateSheet swapRateSheet)
        {
            SwapRateSheet = swapRateSheet;
        }

        public SwapRateSheet SwapRateSheet { get; }

        public Address Address => Addresses.SwapPool;

        public IWorld Swap(IWorld world, IActionContext context, FungibleAssetValue from, Currency to)
        {
            if (from.Currency.Equals(to))
            {
                throw new ArgumentException("Cannot swap the same currency.");
            }
        
            var amountToSwap = AmountToSwap(from, to, out var remainder);
            var newWorld = world
                .TransferAsset(context, context.Signer, Address, from - remainder)
                .TransferAsset(context, Address, context.Signer, amountToSwap);
            return newWorld;
        }

        public FungibleAssetValue AmountToSwap(
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

            return AmountToSwap(from, to, row.Rate.Numerator, row.Rate.Denominator, out remainder);
        }

        public static FungibleAssetValue AmountToSwap(
            FungibleAssetValue from,
            Currency to,
            BigInteger rateNumerator,
            BigInteger rateDenominator,
            out FungibleAssetValue remainder)
        {
            // TODO: Implement this method for remainder calculation with rate
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
