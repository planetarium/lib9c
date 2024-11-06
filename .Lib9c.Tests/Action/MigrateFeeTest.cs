namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Numerics;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class MigrateFeeTest
    {
        private readonly Currency _ncgCurrency;

        public MigrateFeeTest()
        {
#pragma warning disable CS0618
            _ncgCurrency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
        }

        [Fact]
        public void Execute()
        {
            var admin = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            var context = new ActionContext();
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(AdminState.Address, new AdminState(admin, 100).Serialize())
                .SetLegacyState(GoldCurrencyState.Address, new GoldCurrencyState(_ncgCurrency).Serialize());
            var recipient = new PrivateKey().Address;
            var transferData = new List<(Address sender, Address recipient, BigInteger amount)>();
            var amount = FungibleAssetValue.Parse(_ncgCurrency, 0.1m.ToString(CultureInfo.InvariantCulture));
            for (var i = 1; i < 10; i++)
            {
                var address = new PrivateKey().Address;
                var balance = 0.1m * i;
                var fav = FungibleAssetValue.Parse(_ncgCurrency, balance.ToString(CultureInfo.InvariantCulture));
                state = state.MintAsset(context, address, fav);
                transferData.Add((address, recipient, amount.RawValue));
            }

            var action = new MigrateFee
            {
                TransferData = transferData,
                Memo = "memo",
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    BlockIndex = 1L,
                    PreviousState = state,
                    RandomSeed = 0,
                    Signer = admin,
                });

            foreach (var (sender, _, _) in transferData)
            {
                var prevBalance = state.GetBalance(sender, _ncgCurrency);
                Assert.Equal(prevBalance - amount, nextState.GetBalance(sender, _ncgCurrency));
            }

            Assert.Equal(FungibleAssetValue.Parse(_ncgCurrency, "0.9"), nextState.GetBalance(recipient, _ncgCurrency));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PlainValue(bool memo)
        {
            var transferData = new List<(Address sender, Address recipient, BigInteger amount)>();
            // 0.9
            // 1.0
            // 1.1
            for (var i = 9; i < 12; i++)
            {
                var sender = new PrivateKey().Address;
                var recipient = new PrivateKey().Address;
                var amount = FungibleAssetValue.Parse(_ncgCurrency, (0.1m * i).ToString(CultureInfo.InvariantCulture));
                transferData.Add((sender, recipient, amount.RawValue));
            }

            var action = new MigrateFee
            {
                TransferData = transferData,
                Memo = memo ? "memo" : null,
            };

            var des = new MigrateFee();
            des.LoadPlainValue(action.PlainValue);

            for (var i = 0; i < action.TransferData.Count; i++)
            {
                var data = action.TransferData[i];
                Assert.Equal(des.TransferData[i].sender, data.sender);
                Assert.Equal(des.TransferData[i].recipient, data.recipient);
                Assert.Equal(des.TransferData[i].amount, data.amount);
            }

            Assert.Equal(memo, !string.IsNullOrEmpty(des.Memo));
        }

        [Fact]
        public void Execute_Throw_InsufficientBalanceException()
        {
            var admin = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            var context = new ActionContext();
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(AdminState.Address, new AdminState(admin, 100).Serialize())
                .SetLegacyState(GoldCurrencyState.Address, new GoldCurrencyState(_ncgCurrency).Serialize());
            var recipient = new PrivateKey().Address;
            var transferData = new List<(Address sender, Address recipient, BigInteger amount)>();
            var amount = 1 * _ncgCurrency;
            var address = new PrivateKey().Address;
            var balance = 0.1m;
            var fav = FungibleAssetValue.Parse(_ncgCurrency, balance.ToString(CultureInfo.InvariantCulture));
            state = state.MintAsset(context, address, fav);
            transferData.Add((address, recipient, amount.RawValue));

            var action = new MigrateFee
            {
                TransferData = transferData,
                Memo = "memo",
            };

            Assert.Throws<InsufficientBalanceException>(
                () => action.Execute(
                    new ActionContext
                    {
                        BlockIndex = 1L,
                        PreviousState = state,
                        RandomSeed = 0,
                        Signer = admin,
                    }));
        }
    }
}
