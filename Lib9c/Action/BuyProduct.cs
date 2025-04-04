using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Arena;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;
using Log = Serilog.Log;

namespace Nekoyume.Action
{
    [ActionType("buy_product3")]
    public class BuyProduct : GameAction
    {
        // Capacity from Buy limits in NineChronicles
        // https://github.com/planetarium/NineChronicles/blob/v100372-1/nekoyume/Assets/_Scripts/UI/Shop/BuyView.cs#L127
        public const int Capacity = 20;
        public Address AvatarAddress;
        public IEnumerable<IProductInfo> ProductInfos;

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            IWorld states = context.PreviousState;

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("BuyProduct exec started");

            if (!ProductInfos.Any())
            {
                throw new ListEmptyException("ProductInfos was empty.");
            }

            if (ProductInfos.Count() > Capacity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(ProductInfos)} must be less than or equal {Capacity}.");

            }

            if (ProductInfos.Any(p => p.AgentAddress == context.Signer ||
                                      p.AvatarAddress == AvatarAddress))
            {
                throw new InvalidAddressException();
            }

            foreach (var productInfo in ProductInfos)
            {
                productInfo.ValidateType();
            }

            if (!states.TryGetAvatarState(context.Signer, AvatarAddress, out var buyerAvatarState))
            {
                throw new FailedLoadStateException("failed load to buyer avatar state.");
            }

            var materialSheet = states.GetSheet<MaterialItemSheet>();
            foreach (var productInfo in ProductInfos.OrderBy(p => p.ProductId).ThenBy(p =>p.Price))
            {
                var sellerAgentAddress = productInfo.AgentAddress;
                var sellerAvatarAddress = productInfo.AvatarAddress;
                if (!states.TryGetAvatarState(sellerAgentAddress, sellerAvatarAddress, out var sellerAvatarState))
                {
                    throw new FailedLoadStateException($"failed load to seller avatar state.");
                }

                if (productInfo is ItemProductInfo {Legacy: true} itemProductInfo)
                {
                    var purchaseInfo = new PurchaseInfo(itemProductInfo.ProductId, itemProductInfo.TradableId,
                        sellerAgentAddress, sellerAvatarAddress, itemProductInfo.ItemSubType,
                        productInfo.Price);
                    states = Buy_Order(purchaseInfo, context, states, buyerAvatarState, materialSheet, sellerAvatarState);
                }
                else
                {
                    states = Buy(context, productInfo, sellerAvatarAddress, states, sellerAgentAddress, buyerAvatarState, sellerAvatarState, materialSheet);
                }
            }

            states = states.SetAvatarState(AvatarAddress, buyerAvatarState);

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("BuyProduct Total Executed Time: {Elapsed}", ended - started);
            return states;
        }

        private IWorld Buy(IActionContext context, IProductInfo productInfo, Address sellerAvatarAddress,
            IWorld states, Address sellerAgentAddress, AvatarState buyerAvatarState, AvatarState sellerAvatarState,
            MaterialItemSheet materialSheet)
        {
            var productId = productInfo.ProductId;
            var productsStateAddress = ProductsState.DeriveAddress(sellerAvatarAddress);
            var productsState = new ProductsState((List) states.GetLegacyState(productsStateAddress));
            if (!productsState.ProductIds.Contains(productId))
            {
                // sold out or canceled product.
                throw new ProductNotFoundException($"can't find product {productId}");
            }

            productsState.ProductIds.Remove(productId);

            var productAddress = Product.DeriveAddress(productId);
            var product = ProductFactory.DeserializeProduct((List) states.GetLegacyState(productAddress));
            product.Validate(productInfo);

            switch (product)
            {
                case FavProduct favProduct:
                    states = states.TransferAsset(context, productAddress, AvatarAddress, favProduct.Asset);
                    break;
                case ItemProduct itemProduct:
                {
                    switch (itemProduct.TradableItem)
                    {
                        case Costume costume:
                            // Fix RequiredBlockIndex from RegisterProduct0
                            if (costume.RequiredBlockIndex > context.BlockIndex)
                            {
                                costume.RequiredBlockIndex = context.BlockIndex;
                            }
                            buyerAvatarState.UpdateFromAddCostume(costume);
                            break;
                        case ItemUsable itemUsable:
                            // Fix RequiredBlockIndex from RegisterProduct0
                            if (itemUsable.RequiredBlockIndex > context.BlockIndex)
                            {
                                itemUsable.RequiredBlockIndex = context.BlockIndex;
                            }
                            buyerAvatarState.UpdateFromAddItem(itemUsable, false);
                            break;
                        case TradableMaterial tradableMaterial:
                        {
                            buyerAvatarState.UpdateFromAddItem(tradableMaterial, itemProduct.ItemCount, false);
                            break;
                        }
                    }
                }
                    break;
                default:
                    throw new InvalidProductTypeException($"{product} is not support type.");
            }

            var sellerMail = new ProductSellerMail(context.BlockIndex, productId,
                context.BlockIndex, productId, product);
            sellerAvatarState.Update(sellerMail);
            sellerAvatarState.questList.UpdateTradeQuest(TradeType.Sell, product.Price);
            sellerAvatarState.UpdateQuestRewards(materialSheet);

            var buyerMail = new ProductBuyerMail(context.BlockIndex, productId,
                context.BlockIndex, productId, product);
            buyerAvatarState.Update(buyerMail);
            buyerAvatarState.questList.UpdateTradeQuest(TradeType.Buy, product.Price);
            buyerAvatarState.UpdateQuestRewards(materialSheet);

            // Transfer tax.
            var tax = product.Price.DivRem(100, out _) * Action.Buy.TaxRate;
            var taxedPrice = product.Price - tax;

            // Receipt
            var receipt = new ProductReceipt(productId, sellerAvatarAddress, buyerAvatarState.address, product.Price,
                context.BlockIndex);

            var feeAddress = states.GetFeeAddress(context.BlockIndex);

            states = states
                .RemoveLegacyState(productAddress)
                .SetLegacyState(productsStateAddress, productsState.Serialize())
                .SetAvatarState(sellerAvatarAddress, sellerAvatarState)
                .SetLegacyState(ProductReceipt.DeriveAddress(productId), receipt.Serialize())
                .TransferAsset(context, context.Signer, feeAddress, tax)
                .TransferAsset(context, context.Signer, sellerAgentAddress, taxedPrice);

            return states;
        }


        // backward compatibility for order - shared shop state.
        // TODO DELETE THIS METHOD AFTER PRODUCT MIGRATION END.
        private static IWorld Buy_Order(PurchaseInfo purchaseInfo, IActionContext context, IWorld states, AvatarState buyerAvatarState, MaterialItemSheet materialSheet, AvatarState sellerAvatarState)
        {
            Address shardedShopAddress =
                ShardedShopStateV2.DeriveAddress(purchaseInfo.ItemSubType, purchaseInfo.OrderId);
            Address sellerAgentAddress = purchaseInfo.SellerAgentAddress;
            Address sellerAvatarAddress = purchaseInfo.SellerAvatarAddress;
            Guid orderId = purchaseInfo.OrderId;
            Address orderAddress = Order.DeriveAddress(orderId);
            Address digestListAddress = OrderDigestListState.DeriveAddress(sellerAvatarAddress);

            if (!states.TryGetLegacyState(shardedShopAddress, out Bencodex.Types.Dictionary shopStateDict))
            {
                throw new FailedLoadStateException("failed to load shop state");
            }

            if (!states.TryGetLegacyState(orderAddress, out Dictionary rawOrder))
            {
                throw new OrderIdDoesNotExistException($"{orderId}");
            }

            Order order = OrderFactory.Deserialize(rawOrder);

            var shardedShopState = new ShardedShopStateV2(shopStateDict);
            shardedShopState.Remove(order, context.BlockIndex);

            if (!states.TryGetLegacyState(digestListAddress, out Dictionary rawDigestList))
            {
                throw new FailedLoadStateException($"{orderId}");
            }
            var digestList = new OrderDigestListState(rawDigestList);

            // migration method
            sellerAvatarState.inventory.UnlockInvalidSlot(digestList, sellerAgentAddress, sellerAvatarAddress);
            sellerAvatarState.inventory.ReconfigureFungibleItem(digestList, order.TradableId);
            sellerAvatarState.inventory.LockByReferringToDigestList(digestList, order.TradableId, context.BlockIndex);

            digestList.Remove(orderId);

            var errorCode = order.ValidateTransfer(sellerAvatarState, purchaseInfo.TradableId, purchaseInfo.Price, context.BlockIndex);
            switch (errorCode)
            {
                case Action.Buy.ErrorCodeInvalidAddress:
                    throw new InvalidAddressException($"{orderId}");
                case Action.Buy.ErrorCodeInvalidTradableId:
                    throw new InvalidTradableIdException($"{orderId}");
                case Action.Buy.ErrorCodeInvalidPrice:
                    throw new InvalidPriceException($"{orderId}");
                case Action.Buy.ErrorCodeShopItemExpired:
                    throw new ShopItemExpiredException($"{orderId}");
                case Action.Buy.ErrorCodeItemDoesNotExist:
                    throw new ItemDoesNotExistException($"{orderId}");
                case Action.Buy.ErrorCodeInvalidItemType:
                    throw new InvalidItemTypeException($"{orderId}");
            }

            // Check Balance.
            FungibleAssetValue buyerBalance = states.GetBalance(context.Signer, states.GetGoldCurrency());
            if (buyerBalance < order.Price)
            {
                throw new InsufficientBalanceException($"{orderId}", buyerAvatarState.address,
                    buyerBalance);
            }

            var orderReceipt = order.Transfer(sellerAvatarState, buyerAvatarState, context.BlockIndex);

            Address orderReceiptAddress = OrderReceipt.DeriveAddress(orderId);
            if (!(states.GetLegacyState(orderReceiptAddress) is null))
            {
                throw new DuplicateOrderIdException($"{orderId}");
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

            sellerAvatarState.updatedAt = context.BlockIndex;
            sellerAvatarState.blockIndex = context.BlockIndex;

            buyerAvatarState.UpdateQuestRewards(materialSheet);
            sellerAvatarState.UpdateQuestRewards(materialSheet);

            FungibleAssetValue tax = order.GetTax();
            var taxedPrice = order.Price - tax;

            // Transfer tax.
            var feeAddress = states.GetFeeAddress(context.BlockIndex);

            states = states.TransferAsset(
                context,
                context.Signer,
                feeAddress,
                tax);

            // Transfer seller.
            states = states.TransferAsset(
                context,
                context.Signer,
                sellerAgentAddress,
                taxedPrice
            );

            states = states
                .SetLegacyState(digestListAddress, digestList.Serialize())
                .SetLegacyState(orderReceiptAddress, orderReceipt.Serialize())
                .SetAvatarState(sellerAvatarAddress, sellerAvatarState);
            states = states.SetLegacyState(shardedShopAddress, shardedShopState.Serialize());
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["p"] = new List(ProductInfos.Select(p => p.Serialize())),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            var serialized = (List) plainValue["p"];
            ProductInfos = serialized.Cast<List>().Select(ProductFactory.DeserializeProductInfo).ToList();
        }
    }
}
