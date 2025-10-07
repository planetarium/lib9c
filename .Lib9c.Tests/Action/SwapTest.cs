namespace Lib9c.Tests.Action
{
    using System;
    using Lib9c.Action;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Lib9c.TableData.Swap;
    using Lib9c.Tests.Model.Swap;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Xunit;

    public class SwapTest
    {
        private readonly IWorld _initialState;
        private readonly Address _signer;
        private readonly Currency _gold;
        private readonly Currency _mead;

        public SwapTest()
        {
            _gold = Currency.Legacy("NCG", 2, null);
            _mead = Currency.Uncapped("Mead", 18, null);
            _signer = new PrivateKey().Address;
            var csv = @"currency_from,currency_to,rate
NCG;2;null;false;null,Mead;18;null;true;null,1/1
Mead;18;null;true;null,NCG;2;null;false;null,1/1";
            var sheet = new SwapRateSheet();
            sheet.Set(csv);

            _initialState = new World(
                MockWorldState.CreateModern()
                    .SetBalance(_signer, _gold * 1000)
                    .SetBalance(_signer, _mead * 1000)
                    .SetBalance(Addresses.SwapPool, _gold * 1000)
                    .SetBalance(Addresses.SwapPool, _mead * 1000))
                .SetLegacyState(Addresses.GoldCurrency, new GoldCurrencyState(_gold).Serialize())
                .SetLegacyState(Addresses.GetSheetAddress<SwapRateSheet>(), sheet.Serialize());
        }

        [Fact]
        public void Serialization()
        {
            var action = new Swap(_gold * 10, _mead);
            var plainValue = action.PlainValue;

            var deserialized = new Swap();
            deserialized.LoadPlainValue(plainValue);
            Assert.Equal(action.From, deserialized.From);
            Assert.Equal(action.To, deserialized.To);
        }

        [Theory]
        [InlineData(null, 100)]
        [InlineData(null, 1000)]
        [InlineData(typeof(ArgumentOutOfRangeException), 0)]
        [InlineData(typeof(InsufficientBalanceException), 1001)]
        public void Execute_Success_From_Gold_To_Mead(
            Type exception,
            int amountGold)
        {
            var gold = _initialState.GetGoldCurrency();

            try
            {
                new Swap(_gold * amountGold, _mead).Execute(
                    new ActionContext
                    {
                        PreviousState = _initialState,
                        Signer = _signer,
                    });
            }
            catch (Exception e)
            {
                Assert.Equal(exception, e.GetType());
            }
        }

        private IWorld Execute(
            IWorld previousState,
            FungibleAssetValue from,
            Currency to,
            Address signer,
            long amount)
        {
            var initialPoolFromBalance = previousState.GetBalance(Addresses.SwapPool, from.Currency);
            var initialPoolToBalance = previousState.GetBalance(Addresses.SwapPool, to);
            var initialSignerFromBalance = previousState.GetBalance(signer, from.Currency);
            var initialSignerToBalance = previousState.GetBalance(signer, to);

            var action = new Swap(from, to);
            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = previousState,
                    Signer = signer,
                });

            var swapRateSheet = previousState.GetSheet<SwapRateSheet>();
            swapRateSheet.TryGetValue(new SwapRateSheet.CurrencyPair(from.Currency, to), out var row);
            var expectedSwap = SwapPoolTest.ApplyDecimalRate(from, to, row.Rate);

            // Assert
            Assert.Equal(expectedSwap, nextState.GetBalance(signer, to) - initialSignerToBalance);
            Assert.Equal(expectedSwap, nextState.GetBalance(Addresses.SwapPool, to) - initialPoolToBalance);

            return nextState;
        }
    }
}
