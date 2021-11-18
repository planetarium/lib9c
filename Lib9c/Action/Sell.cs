using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using MessagePack;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("sell10")]
    [MessagePackObject]
    public class Sell : GameAction
    {
        [Key(1)]
#pragma warning disable MsgPack003
        public Address sellerAvatarAddress;
#pragma warning restore MsgPack003
        [Key(2)]
        public Guid tradableId;
        [Key(3)]
        public int count;
        [Key(4)]
#pragma warning disable MsgPack003
        public FungibleAssetValue price;
#pragma warning restore MsgPack003
        [Key(5)]
        public ItemSubType itemSubType;
        [Key(6)]
        public Guid orderId;

        public Sell()
        {
        }

        [SerializationConstructor]
        public Sell(
            Guid guid,
            Address sellerAvatarAddress,
            Guid tradableId,
            int count,
            FungibleAssetValue price,
            ItemSubType itemSubType,
            Guid orderId) : base(guid)
        {
            this.sellerAvatarAddress = sellerAvatarAddress;
            this.tradableId = tradableId;
            this.count = count;
            this.price = price;
            this.itemSubType = itemSubType;
            this.orderId = orderId;
        }

        [IgnoreMember]
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

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var inventoryAddress = sellerAvatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = sellerAvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = sellerAvatarAddress.Derive(LegacyQuestListKey);
            Address shopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, orderId);
            Address itemAddress = Addresses.GetItemAddress(tradableId);
            Address orderAddress = Order.DeriveAddress(orderId);
            Address orderReceiptAddress = OrderDigestListState.DeriveAddress(sellerAvatarAddress);
            if (context.Rehearsal)
            {
                return states
                    .SetState(context.Signer, MarkChanged)
                    .SetState(shopAddress, MarkChanged)
                    .SetState(itemAddress, MarkChanged)
                    .SetState(orderAddress, MarkChanged)
                    .SetState(orderReceiptAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(sellerAvatarAddress, MarkChanged);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, sellerAvatarAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Sell exec started", addressesHex);

            if (price.Sign < 0)
            {
                throw new InvalidPriceException(
                    $"{addressesHex}Aborted as the price is less than zero: {price}.");
            }

            if (!states.TryGetAgentAvatarStatesV2(
                context.Signer,
                sellerAvatarAddress,
                out _,
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

            Order order = OrderFactory.Create(context.Signer, sellerAvatarAddress, orderId, price, tradableId,
                context.BlockIndex, itemSubType, count);
            order.Validate(avatarState, count);

            ITradableItem tradableItem = order.Sell(avatarState);

            var shardedShopState = states.TryGetState(shopAddress, out Dictionary serializedState)
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

            var orderReceiptList = states.TryGetState(orderReceiptAddress, out Dictionary receiptDict)
                ? new OrderDigestListState(receiptDict)
                : new OrderDigestListState(orderReceiptAddress);

            orderReceiptList.Add(orderDigest);

            states = states.SetState(orderReceiptAddress, orderReceiptList.Serialize());
            states = states
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(sellerAvatarAddress, avatarState.SerializeV2());
            sw.Stop();
            Log.Verbose("{AddressesHex}Sell Set AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            states = states
                .SetState(itemAddress, tradableItem.Serialize())
                .SetState(orderAddress, order.Serialize())
                .SetState(shopAddress, shardedShopState.Serialize());
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Sell Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            Log.Verbose(
                "{AddressesHex}Sell Total Executed Time: {Elapsed}",
                addressesHex,
                ended - started);

            return states;
        }
    }
}
