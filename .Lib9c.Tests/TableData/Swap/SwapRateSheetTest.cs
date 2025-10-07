namespace Lib9c.Tests.TableData.Swap
{
    using System;
    using System.Collections.Immutable;
    using System.Numerics;
    using Lib9c.TableData.Swap;
    using Lib9c.Tests.Fixtures.TableCSV.Swap;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Xunit;

    public class SwapRateSheetTest
    {
        private readonly Currency testA
            = Currency.Legacy("TEST_A", 2, null);

        private readonly Currency testB
            = Currency.Uncapped("TEST_B", 18, null);

        private readonly Currency testC
            = Currency.Legacy(
                "TEST_C",
                2,
                ImmutableHashSet.Create(new Address("0000000000000000000000000000000000000000")));

        private readonly Currency testD
            = Currency.Uncapped(
                "TEST_D",
                18,
                ImmutableHashSet.Create(
                    new Address("0000000000000000000000000000000000000001"),
                    new Address("0000000000000000000000000000000000000002")));

        private readonly Currency testE
            = Currency.Capped("TEST_E", 2, (new BigInteger(100), new BigInteger(99)), null);

        private readonly Currency testF
            = Currency.Capped("TEST_F", 18, (new BigInteger(1000), new BigInteger(1000)), null);

        [Fact]
        public void Set_Success()
        {
            var sheet = new SwapRateSheet();
            sheet.Set(SwapRateSheetFixtures.Default);
            Assert.Equal(4, sheet.Count);
            Assert.True(sheet.TryGetValue(new SwapRateSheet.CurrencyPair(testA, testB), out var rowAB));
            Assert.True(sheet.TryGetValue(new SwapRateSheet.CurrencyPair(testC, testD), out var rowCD));
            Assert.True(sheet.TryGetValue(new SwapRateSheet.CurrencyPair(testE, testF), out var rowEF));
            Assert.Equal(testA, rowAB.Pair.From);
            Assert.Equal(testB, rowAB.Pair.To);
            Assert.Equal(2, rowAB.Rate.Numerator);
            Assert.Equal(3, rowAB.Rate.Denominator);
            Assert.Equal(testC, rowCD.Pair.From);
            Assert.Equal(testD, rowCD.Pair.To);
            Assert.Equal(3, rowCD.Rate.Numerator);
            Assert.Equal(2, rowCD.Rate.Denominator);
            Assert.Equal(testE, rowEF.Pair.From);
            Assert.Equal(testF, rowEF.Pair.To);
            Assert.Equal(1, rowEF.Rate.Numerator);
            Assert.Equal(1, rowEF.Rate.Denominator);
        }

        [Fact]
        public void Set_Throw_Invalid_Currency()
        {
            var sheet = new SwapRateSheet();
            Assert.Throws<ArgumentException>(() => sheet.Set(SwapRateSheetFixtures.CappedLegacyFrom));
            Assert.Throws<ArgumentException>(() => sheet.Set(SwapRateSheetFixtures.CapExceedsDecimalFrom));
        }
    }
}
