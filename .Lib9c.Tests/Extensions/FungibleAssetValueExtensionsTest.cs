namespace Lib9c.Tests.Extensions
{
    using Libplanet.Assets;
    using Nekoyume.Extensions;
    using Xunit;

    public class FungibleAssetValueExtensionsTest
    {
        [Fact]
        public void IsAvailableInMarket_Returns_False_With_Single_Currency()
        {
            var availableCurrency =
                Currency.Legacy("NCG", 2, minters: null);
            var notAvailableCurrency =
                Currency.Legacy("KRW", 0, minters: null);
            var value =
                new FungibleAssetValue(notAvailableCurrency, 100, 0);
            Assert.False(value.IsAvailableInMarket(availableCurrency));
        }

        [Fact]
        public void IsAvailableInMarket_Returns_False_With_Multiple_Currencies()
        {
            var availableCurrency =
                Currency.Legacy("NCG", 2, minters: null);
            var availableCurrency2 =
                Currency.Legacy("KRW", 0, minters: null);
            var notAvailableCurrency =
                Currency.Legacy("USD", 0, minters: null);
            var value =
                new FungibleAssetValue(notAvailableCurrency, 100, 0);
            Assert.False(value.IsAvailableInMarket(
                availableCurrency,
                availableCurrency2));
        }

        [Fact]
        public void IsAvailableInMarket_Returns_True_With_Single_Currency()
        {
            var availableCurrency =
                Currency.Legacy("NCG", 2, minters: null);
            var value =
                new FungibleAssetValue(availableCurrency, 100, 0);
            Assert.True(value.IsAvailableInMarket(availableCurrency));
        }

        [Fact]
        public void IsAvailableInMarket_Returns_True_With_Multiple_Currencies()
        {
            var availableCurrency =
                Currency.Legacy("NCG", 2, minters: null);
            var availableCurrency2 =
                Currency.Legacy("KRW", 0, minters: null);
            var value =
                new FungibleAssetValue(availableCurrency, 100, 0);
            Assert.True(value.IsAvailableInMarket(
                availableCurrency,
                availableCurrency2));
        }
    }
}
