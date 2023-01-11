using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Bencodex.Types;
using Lib9c;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [ActionType("sell_fungible_asset")]
    public class SellFungibleAsset: GameAction
    {
        public Address SellerAvatarAddress;
        public FungibleAssetValue Price;
        public FungibleAssetValue Asset;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var addressesHex = GetSignerAndOtherAddressesHex(context, SellerAvatarAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Sell exec started", addressesHex);

            var ncg = states.GetGoldCurrency();
            if (!Price.Currency.Equals(ncg) ||
                !Price.MinorUnit.IsZero ||
                Price.Sign < 0)
            {
                throw new InvalidPriceException(
                    $"{addressesHex}Aborted as the price is less than zero: {Price}.");
            }

            var orderId = context.Random.GenerateRandomGuid();
            var tradableId = context.Random.GenerateRandomGuid();
            Address shopAddress = ShardedShopStateV2.DeriveAddress(ItemSubType.FungibleAssetValue, orderId);
            Address orderAddress = Order.DeriveAddress(orderId);
            Address orderReceiptAddress = OrderDigestListState.DeriveAddress(SellerAvatarAddress);

            var order = OrderFactory.CreateFungibleAssetValueOrder(context.Signer,
                SellerAvatarAddress, orderId, Price, tradableId, context.BlockIndex, Asset);
            order.Validate(states);
            states = order.Sell(states);

            var shardedShopState = states.TryGetState(shopAddress, out Dictionary serializedState)
                ? new ShardedShopStateV2(serializedState)
                : new ShardedShopStateV2(shopAddress);

            sw.Stop();
            Log.Verbose(
                "{AddressesHex}Sell Get ShardedShopState: {Elapsed}",
                addressesHex,
                sw.Elapsed);
            sw.Restart();

            var runeSheet = states.GetSheet<RuneSheet>();
            OrderDigest orderDigest = order.Digest(runeSheet);
            shardedShopState.Add(orderDigest, context.BlockIndex);

            var orderReceiptList =
                states.TryGetState(orderReceiptAddress, out Dictionary receiptDict)
                    ? new OrderDigestListState(receiptDict)
                    : new OrderDigestListState(orderReceiptAddress);

            orderReceiptList.Add(orderDigest);

            states = states
                .SetState(orderReceiptAddress, orderReceiptList.Serialize())
                .SetState(orderAddress, order.Serialize())
                .SetState(shopAddress, shardedShopState.Serialize());
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Sell Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            Log.Debug(
                "{AddressesHex}Sell Total Executed Time: {Elapsed}",
                addressesHex,
                ended - started);

            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["sa"] = SellerAvatarAddress.Serialize(),
                ["p"] = Price.Serialize(),
                ["a"] = Asset.Serialize(),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            SellerAvatarAddress = plainValue["sa"].ToAddress();
            Price = plainValue["p"].ToFungibleAssetValue();
            Asset = plainValue["a"].ToFungibleAssetValue();
        }
    }
}
