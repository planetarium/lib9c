namespace Lib9c.Tests.Action
{
    using Lib9c.Model.Order;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class SellFungibleAssetTest
    {
        [Fact]
        public void Execute()
        {
            var random = new TestRandom();
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var avatarAddress = new PrivateKey().ToAddress();
            var currency = Currency.Legacy("NCG", 2, minters: null);
            var rune = RuneHelper.DailyRewardRune;
            IAccountStateDelta states = new State()
                .SetState(Addresses.GetSheetAddress<RuneSheet>(), tableSheets.RuneSheet.Serialize())
                .SetState(Addresses.GoldCurrency, new GoldCurrencyState(currency).Serialize())
                .MintAsset(avatarAddress, rune * 100);

            var action = new SellFungibleAsset
            {
                SellerAvatarAddress = avatarAddress,
                Price = 100 * currency,
                Asset = 100 * rune,
            };

            var nextState = action.Execute(new ActionContext
            {
                Signer = default,
                Rehearsal = false,
                BlockIndex = 0L,
                PreviousStates = states,
                Random = random,
            });

            var orderId = new TestRandom().GenerateRandomGuid();

            Address shopAddress = ShardedShopStateV2.DeriveAddress(ItemSubType.FungibleAssetValue, orderId);
            Address orderAddress = Order.DeriveAddress(orderId);
            Address orderReceiptAddress = OrderDigestListState.DeriveAddress(avatarAddress);
            Assert.NotNull(nextState.GetState(shopAddress));
            Assert.NotNull(nextState.GetState(orderAddress));
            Assert.NotNull(nextState.GetState(orderReceiptAddress));
            Assert.Equal(0 * rune, nextState.GetBalance(avatarAddress, rune));
            Assert.Equal(100 * rune, nextState.GetBalance(orderAddress, rune));
        }
    }
}
