using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Bencodex.Types;
using Lib9c.Abstractions;
using Lib9c.Model.Order;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1640
    /// </summary>
    [Serializable]
    [ActionType("sell12")]
    [ActionObsolete(ActionObsoleteConfig.V200030ObsoleteIndex)]
    public class Sell : GameAction, ISellV2
    {
        public Address sellerAvatarAddress;
        public Guid tradableId;
        public int count;
        public FungibleAssetValue price;
        public ItemSubType itemSubType;
        public Guid orderId;

        Address ISellV2.SellerAvatarAddress => sellerAvatarAddress;
        Guid ISellV2.TradableId => tradableId;
        int ISellV2.Count => count;
        FungibleAssetValue ISellV2.Price => price;
        string ISellV2.ItemSubType => itemSubType.ToString();
        Guid? ISellV2.OrderId => orderId;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [SellerAvatarAddressKey] = sellerAvatarAddress.Serialize(),
                [ItemIdKey] = tradableId.Serialize(),
                [ItemCountKey] = count.Serialize(),
                [PriceKey] = price.Serialize(),
                [ItemSubTypeKey] = itemSubType.Serialize(),
                [OrderIdKey] = orderId.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            sellerAvatarAddress = plainValue[SellerAvatarAddressKey].ToAddress();
            tradableId = plainValue[ItemIdKey].ToGuid();
            count = plainValue[ItemCountKey].ToInteger();
            price = plainValue[PriceKey].ToFungibleAssetValue();
            itemSubType = plainValue[ItemSubTypeKey].ToEnum<ItemSubType>();
            orderId = plainValue[OrderIdKey].ToGuid();
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            Address shopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, orderId);
            Address itemAddress = Addresses.GetItemAddress(tradableId);
            Address orderAddress = Order.DeriveAddress(orderId);
            Address orderReceiptAddress = OrderDigestListState.DeriveAddress(sellerAvatarAddress);

            CheckObsolete(ActionObsoleteConfig.V200030ObsoleteIndex, context);
            if (!(states.GetLegacyState(Addresses.Market) is null))
            {
                throw new ActionObsoletedException("Sell action is obsoleted. please use SellProduct.");
            }
            var addressesHex = GetSignerAndOtherAddressesHex(context, sellerAvatarAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Sell exec started", addressesHex);

            var ncg = states.GetGoldCurrency();
            if (!price.Currency.Equals(ncg) ||
                !price.MinorUnit.IsZero ||
                price.Sign < 0)
            {
                throw new InvalidPriceException(
                    $"{addressesHex}Aborted as the price is less than zero: {price}.");
            }

            if (states.GetAgentState(context.Signer) is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the agent state of the signer was failed to load.");
            }

            if (!states.TryGetAvatarState(
                    context.Signer,
                    sellerAvatarAddress,
                    out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose(
                "{AddressesHex}Sell Get AgentAvatarStates: {Elapsed}",
                addressesHex,
                sw.Elapsed);
            sw.Restart();

            if (!avatarState.worldInformation.IsStageCleared(
                    GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInShop,
                    current);
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}Sell IsStageCleared: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            Order order = OrderFactory.Create(
                context.Signer,
                sellerAvatarAddress,
                orderId,
                price,
                tradableId,
                context.BlockIndex,
                itemSubType,
                count);
            order.Validate(avatarState, count);

            ITradableItem tradableItem = order.Sell(avatarState);

            var shardedShopState = states.TryGetLegacyState(shopAddress, out Dictionary serializedState)
                ? new ShardedShopStateV2(serializedState)
                : new ShardedShopStateV2(shopAddress);

            sw.Stop();
            Log.Verbose(
                "{AddressesHex}Sell Get ShardedShopState: {Elapsed}",
                addressesHex,
                sw.Elapsed);
            sw.Restart();

            var costumeStatSheet = states.GetSheet<CostumeStatSheet>();
            OrderDigest orderDigest = order.Digest(avatarState, costumeStatSheet);
            shardedShopState.Add(orderDigest, context.BlockIndex);

            avatarState.updatedAt = context.BlockIndex;
            avatarState.blockIndex = context.BlockIndex;

            var orderReceiptList =
                states.TryGetLegacyState(orderReceiptAddress, out Dictionary receiptDict)
                    ? new OrderDigestListState(receiptDict)
                    : new OrderDigestListState(orderReceiptAddress);

            orderReceiptList.Add(orderDigest);

            states = states
                .SetLegacyState(orderReceiptAddress, orderReceiptList.Serialize())
                .SetAvatarState(sellerAvatarAddress, avatarState);
            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states
                .SetLegacyState(itemAddress, tradableItem.Serialize())
                .SetLegacyState(orderAddress, order.Serialize())
                .SetLegacyState(shopAddress, shardedShopState.Serialize());
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Sell Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            Log.Debug(
                "{AddressesHex}Sell Total Executed Time: {Elapsed}",
                addressesHex,
                ended - started);

            return states;
        }
    }
}
