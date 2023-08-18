using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Lib9c.Model.Order;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Extensions;
using Nekoyume.Model;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Updated at https://github.com/planetarium/lib9c/pull/1164
    /// </summary>
    [Serializable]
    [ActionType("buy12")]
    public class Buy : GameAction, IBuy5, IBuyV2
    {


        public const int TaxRate = 8;
        public const int ErrorCodeFailedLoadingState = 1;
        public const int ErrorCodeItemDoesNotExist = 2;
        public const int ErrorCodeShopItemExpired = 3;
        public const int ErrorCodeInsufficientBalance = 4;
        public const int ErrorCodeInvalidAddress = 5;
        public const int ErrorCodeInvalidPrice = 6;
        public const int ErrorCodeInvalidOrderId = 7;
        public const int ErrorCodeInvalidTradableId = 8;
        public const int ErrorCodeInvalidItemType = 9;
        public const int ErrorCodeDuplicateSell = 10;

        public Address buyerAvatarAddress { get; set; }
        public List<(Guid orderId, int errorCode)> errors = new List<(Guid orderId, int errorCode)>();
        public IEnumerable<PurchaseInfo> purchaseInfos;
        IEnumerable<IPurchaseInfo> IBuy5.purchaseInfos => purchaseInfos;

        Address IBuyV2.BuyerAvatarAddress => buyerAvatarAddress;
        IEnumerable<IValue> IBuyV2.PurchaseInfos => purchaseInfos.Select(x => x.Serialize());

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            [BuyerAvatarAddressKey] = buyerAvatarAddress.Serialize(),
            [PurchaseInfosKey] = purchaseInfos
                .OrderBy(p => p.OrderId)
                .Select(p => p.Serialize())
                .Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            buyerAvatarAddress = plainValue[BuyerAvatarAddressKey].ToAddress();
            purchaseInfos = plainValue[PurchaseInfosKey].ToList(value => new PurchaseInfo((Dictionary)value));
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            if (context.Rehearsal)
            {
                return context.PreviousState;
            }

            IActionContext ctx = context;
            var world = ctx.PreviousState;
            var buyerInventoryAddress = buyerAvatarAddress.Derive(LegacyInventoryKey);
            var buyerWorldInformationAddress = buyerAvatarAddress.Derive(LegacyWorldInformationKey);
            var buyerQuestListAddress = buyerAvatarAddress.Derive(LegacyQuestListKey);

            var addressesHex = GetSignerAndOtherAddressesHex(context, buyerAvatarAddress);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Buy exec started", addressesHex);

            if (!AvatarModule.TryGetAvatarStateV2(
                    world,
                    ctx.Signer,
                    buyerAvatarAddress,
                    out var buyerAvatarState,
                    out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the buyer was failed to load.");
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Get Buyer AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!buyerAvatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                buyerAvatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInShop, current);
            }

            MaterialItemSheet materialSheet = LegacyModule.GetSheet<MaterialItemSheet>(world);

            foreach (var purchaseInfo in purchaseInfos)
            {
                Address shardedShopAddress =
                    ShardedShopStateV2.DeriveAddress(purchaseInfo.ItemSubType, purchaseInfo.OrderId);
                Address sellerAgentAddress = purchaseInfo.SellerAgentAddress;
                Address sellerAvatarAddress = purchaseInfo.SellerAvatarAddress;
                Address sellerInventoryAddress = sellerAvatarAddress.Derive(LegacyInventoryKey);
                var sellerWorldInformationAddress = sellerAvatarAddress.Derive(LegacyWorldInformationKey);
                Address sellerQuestListAddress = sellerAvatarAddress.Derive(LegacyQuestListKey);
                Guid orderId = purchaseInfo.OrderId;
                Address orderAddress = Order.DeriveAddress(orderId);
                Address digestListAddress = OrderDigestListState.DeriveAddress(sellerAvatarAddress);

                if (purchaseInfo.SellerAgentAddress == ctx.Signer)
                {
                    errors.Add((orderId, ErrorCodeInvalidAddress));
                    continue;
                }

                if (!LegacyModule.TryGetState(
                        world,
                        shardedShopAddress,
                        out Bencodex.Types.Dictionary shopStateDict))
                {
                    errors.Add((orderId, ErrorCodeFailedLoadingState));
                    continue;
                }

                if (!LegacyModule.TryGetState(world, orderAddress, out Dictionary rawOrder))
                {
                    errors.Add((orderId, ErrorCodeInvalidOrderId));
                    continue;
                }

                Order order = OrderFactory.Deserialize(rawOrder);

                var shardedShopState = new ShardedShopStateV2(shopStateDict);

                try
                {
                    shardedShopState.Remove(order, context.BlockIndex);
                }
                catch (OrderIdDoesNotExistException)
                {
                    errors.Add((orderId, ErrorCodeInvalidOrderId));
                    continue;
                }

                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Get ShopState: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();

                Log.Verbose(
                    "{AddressesHex}Execute Buy; buyer: {Buyer} seller: {Seller}",
                    addressesHex,
                    buyerAvatarAddress,
                    sellerAvatarAddress);


                if (!AvatarModule.TryGetAvatarStateV2(
                        world,
                        sellerAgentAddress,
                        sellerAvatarAddress,
                        out var sellerAvatarState,
                        out _))
                {
                    errors.Add((orderId, ErrorCodeFailedLoadingState));
                    continue;
                }

                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Get Seller AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();

                if (!LegacyModule.TryGetState(
                        world,
                        digestListAddress,
                        out Dictionary rawDigestList))
                {
                    errors.Add((orderId, ErrorCodeFailedLoadingState));
                    continue;
                }

                var digestList = new OrderDigestListState(rawDigestList);

                // migration method
                sellerAvatarState.inventory.UnlockInvalidSlot(digestList, sellerAgentAddress, sellerAvatarAddress);
                sellerAvatarState.inventory.ReconfigureFungibleItem(digestList, order.TradableId);
                sellerAvatarState.inventory.LockByReferringToDigestList(digestList, order.TradableId, context.BlockIndex);
                //

                digestList.Remove(orderId);

                var errorCode = order.ValidateTransfer(sellerAvatarState, purchaseInfo.TradableId, purchaseInfo.Price, context.BlockIndex);
                if (errorCode != 0)
                {
                    errors.Add((orderId, errorCode));
                    continue;
                }

                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Get Item: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();

                // Check Balance.
                FungibleAssetValue buyerBalance = LegacyModule.GetBalance(
                    world,
                    context.Signer,
                    LegacyModule.GetGoldCurrency(world));
                if (buyerBalance < order.Price)
                {
                    errors.Add((orderId, ErrorCodeInsufficientBalance));
                    continue;
                }

                OrderReceipt orderReceipt;
                try
                {
                    orderReceipt = order.Transfer(sellerAvatarState, buyerAvatarState, context.BlockIndex);
                }
                catch (ItemDoesNotExistException)
                {
                    errors.Add((orderId, ErrorCodeItemDoesNotExist));
                    continue;
                }

                Address orderReceiptAddress = OrderReceipt.DeriveAddress(orderId);
                if (!(LegacyModule.GetState(world, orderReceiptAddress) is null))
                {
                    errors.Add((orderId, ErrorCodeDuplicateSell));
                    continue;
                }

                var expirationMail = sellerAvatarState.mailBox.OfType<OrderExpirationMail>()
                    .FirstOrDefault(m => m.OrderId.Equals(orderId));
                if (!(expirationMail is null))
                {
                    sellerAvatarState.mailBox.Remove(expirationMail);
                }

                var orderSellerMail = new OrderSellerMail(
                    context.BlockIndex,
                    orderId,
                    context.BlockIndex,
                    orderId
                );
                var orderBuyerMail = new OrderBuyerMail(
                    context.BlockIndex,
                    orderId,
                    context.BlockIndex,
                    orderId
                );

                buyerAvatarState.Update(orderBuyerMail);
                sellerAvatarState.Update(orderSellerMail);

                // // Update quest.
                buyerAvatarState.questList.UpdateTradeQuest(TradeType.Buy, order.Price);
                sellerAvatarState.questList.UpdateTradeQuest(TradeType.Sell, order.Price);

                sellerAvatarState.updatedAt = ctx.BlockIndex;
                sellerAvatarState.blockIndex = ctx.BlockIndex;

                buyerAvatarState.UpdateQuestRewards(materialSheet);
                sellerAvatarState.UpdateQuestRewards(materialSheet);

                FungibleAssetValue tax = order.GetTax();
                var taxedPrice = order.Price - tax;

                // Transfer tax.
                var arenaSheet = LegacyModule.GetSheet<ArenaSheet>(world);
                var arenaData = arenaSheet.GetRoundByBlockIndex(context.BlockIndex);
                var feeStoreAddress = Addresses.GetShopFeeAddress(arenaData.ChampionshipId, arenaData.Round);
                world = LegacyModule.TransferAsset(
                    world,
                    context,
                    context.Signer,
                    feeStoreAddress,
                    tax);

                // Transfer seller.
                world = LegacyModule.TransferAsset(
                    world,
                    context,
                    context.Signer,
                    sellerAgentAddress,
                    taxedPrice
                );

                world = LegacyModule.SetState(world, digestListAddress, digestList.Serialize());
                world = LegacyModule.SetState(world, orderReceiptAddress, orderReceipt.Serialize());
                world = LegacyModule.SetState(
                    world,
                    sellerInventoryAddress,
                    sellerAvatarState.inventory.Serialize());
                world = LegacyModule.SetState(
                    world,
                    sellerWorldInformationAddress,
                    sellerAvatarState.worldInformation.Serialize());
                world = LegacyModule.SetState(
                    world,
                    sellerQuestListAddress,
                    sellerAvatarState.questList.Serialize());
                world = LegacyModule.SetState(
                    world,
                    sellerAvatarAddress,
                    sellerAvatarState.SerializeV2());
                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Set Seller AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();
                world = LegacyModule.SetState(
                    world,
                    shardedShopAddress,
                    shardedShopState.Serialize());
                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            }

            buyerAvatarState.updatedAt = ctx.BlockIndex;
            buyerAvatarState.blockIndex = ctx.BlockIndex;

            world = LegacyModule.SetState(
                world,
                buyerInventoryAddress,
                buyerAvatarState.inventory.Serialize());
            world = LegacyModule.SetState(
                world,
                buyerWorldInformationAddress,
                buyerAvatarState.worldInformation.Serialize());
            world = LegacyModule.SetState(
                world,
                buyerQuestListAddress,
                buyerAvatarState.questList.Serialize());
            world = AvatarModule.SetAvatarState(world, buyerAvatarAddress, buyerAvatarState);
            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Set Buyer AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Buy Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return world;
        }
    }
}
