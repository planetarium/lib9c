namespace Lib9c.Tests.Model.Swap
{
    using System;
    using Lib9c.Tests.Action;
    using Lib9c.Tests.Fixtures.TableCSV.Swap;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Model.Swap;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Swap;
    using Xunit;

    public class SwapPoolTest
    {
        [Theory]
        [InlineData(10)]
        [InlineData(200)]
        [InlineData(4321)]
        [InlineData(12345)]
        public void AmountToSwap(int amount)
        {
            // Arrange
            var sheet = new SwapRateSheet();
            sheet.Set(SwapRateSheetFixtures.Default);
            var swapPool = new SwapPool(sheet);
            var from = Currency.Legacy("TEST_A", 2, null);
            var to = Currency.Uncapped("TEST_B", 18, null);
            var currencyPair = new SwapRateSheet.CurrencyPair(from, to);
            Assert.True(sheet.TryGetValue(currencyPair, out var row));

            // Act
            var fromFAV = from * amount;
            var result = swapPool.AmountToSwap(fromFAV, to, out var rem);
            var rateApplied = (decimal)amount * (decimal)row.Rate.Numerator / (decimal)row.Rate.Denominator;
            var rateAppliedString = rateApplied.ToString();
            var headLength = rateAppliedString.Split(".")[0].Length;
            var expected = ApplyDecimalRate(fromFAV, to, row.Rate);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(200)]
        [InlineData(4321)]
        [InlineData(12345)]
        public void Swap(int amount)
        {
            // Arrange
            var sheet = new SwapRateSheet();
            sheet.Set(SwapRateSheetFixtures.Default);
            var swapPool = new SwapPool(sheet);
            var from = Currency.Legacy("TEST_A", 2, null);
            var to = Currency.Uncapped("TEST_B", 18, null);
            var currencyPair = new SwapRateSheet.CurrencyPair(from, to);
            Assert.True(sheet.TryGetValue(currencyPair, out var row));

            var signer = new PrivateKey().Address;
            var initialPoolBalance = to * 1000000;
            var world = new World(
                MockWorldState.CreateModern()
                    .SetBalance(signer, from * amount)
                    .SetBalance(Addresses.SwapPool, initialPoolBalance))
                .SetLegacyState(Addresses.GetSheetAddress<SwapRateSheet>(), sheet.Serialize());

            // Act
            var fromFAV = from * amount;
            var result = swapPool.Swap(world, new ActionContext { Signer = signer }, fromFAV, to);
            var expectedSwap = ApplyDecimalRate(fromFAV, to, row.Rate);

            // Assert
            Assert.Equal(expectedSwap, result.GetBalance(signer, to));
            Assert.Equal(initialPoolBalance - expectedSwap, result.GetBalance(Addresses.SwapPool, to));
        }

        [Fact]
        public void AmountToSwap_ThrowSheetRowNotFoundException()
        {
            // Arrange
            var sheet = new SwapRateSheet();
            sheet.Set(SwapRateSheetFixtures.Default);
            var swapPool = new SwapPool(sheet);
            var from = Currency.Legacy("TEST_A", 2, null);
            var to = Currency.Uncapped("TEST_C", 18, null);
            var currencyPair = new SwapRateSheet.CurrencyPair(from, to);

            // Act
            var fromFAV = from * 10;

            // Assert
            Assert.Throws<SheetRowNotFoundException>(() => swapPool.AmountToSwap(fromFAV, to, out var rem));
        }

        internal static FungibleAssetValue ApplyDecimalRate(FungibleAssetValue fromFAV, Currency toCurrency, SwapRateSheet.Fraction rate)
        {
            var from = decimal.Parse(fromFAV.GetQuantityString());
            var rateApplied = from * (decimal)rate.Numerator / (decimal)rate.Denominator;
            var rateAppliedString = rateApplied.ToString();
            var splitted = rateAppliedString.Split(".");
            string expectedString;
            switch (splitted.Length)
            {
                case 1:
                    expectedString = splitted[0];
                    break;
                case 2:
                    var head = splitted[0];
                    var tail = splitted[1];
                    if (tail.Length > toCurrency.DecimalPlaces)
                    {
                        tail = tail.Substring(0, toCurrency.DecimalPlaces);
                    }

                    expectedString = string.Join(".", head, tail);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return FungibleAssetValue.Parse(toCurrency, expectedString);
        }
    }
}
