using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;
using Log = Serilog.Log;

namespace Nekoyume.Action
{
    [ActionType("buy_product")]
    public class BuyProduct : GameAction
    {
        public Address AvatarAddress;
        public IEnumerable<ProductInfo> ProductInfos;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("BuyProduct exec started");

            if (!states.TryGetAvatarStateV2(context.Signer, AvatarAddress, out var buyerAvatarState, out var migrationRequired))
            {
                throw new FailedLoadStateException("");
            }

            var materialSheet = states.GetSheet<MaterialItemSheet>();
            foreach (var productInfo in ProductInfos.OrderBy(p => p.ProductId).ThenBy(p =>p.Price))
            {
                var sellerAgentAddress = productInfo.AgentAddress;
                var sellerAvatarAddress = productInfo.AvatarAddress;
                if (!states.TryGetAvatarStateV2(sellerAgentAddress, sellerAvatarAddress,
                        out var sellerAvatarState, out var sellerMigrationRequired))
                {
                    throw new InvalidAddressException();
                }
                var productId = productInfo.ProductId;
                var productsStateAddress = ProductsState.DeriveAddress(sellerAvatarAddress);
                var productsState = new ProductsState((List)states.GetState(productsStateAddress));
                if (!productsState.ProductIds.Contains(productId))
                {
                    // sold out or canceled product.
                    throw new ProductNotFoundException($"can't find product {productId}");
                }

                productsState.ProductIds.Remove(productId);

                var productAddress = Product.DeriveAddress(productId);
                var product = ProductFactory.Deserialize((List) states.GetState(productAddress));
                if (product.SellerAgentAddress != sellerAgentAddress ||
                    product.SellerAvatarAddress != sellerAvatarAddress)
                {
                    throw new InvalidAddressException();
                }

                switch (product)
                {
                    case FavProduct favProduct:
                        states = states.TransferAsset(productAddress, AvatarAddress, favProduct.Asset);
                        break;
                    case ItemProduct itemProduct:
                    {
                        switch (itemProduct.TradableItem)
                        {
                            case Costume costume:
                                buyerAvatarState.UpdateFromAddCostume(costume, false);
                                break;
                            case ItemUsable itemUsable:
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
                    context.BlockIndex, productId);
                sellerAvatarState.Update(sellerMail);
                sellerAvatarState.questList.UpdateTradeQuest(TradeType.Sell, product.Price);
                sellerAvatarState.UpdateQuestRewards(materialSheet);

                var buyerMail = new ProductBuyerMail(context.BlockIndex, productId,
                    context.BlockIndex, productId
                );
                buyerAvatarState.Update(buyerMail);
                buyerAvatarState.questList.UpdateTradeQuest(TradeType.Buy, product.Price);
                buyerAvatarState.UpdateQuestRewards(materialSheet);

                // Transfer tax.
                var arenaSheet = states.GetSheet<ArenaSheet>();
                var arenaData = arenaSheet.GetRoundByBlockIndex(context.BlockIndex);
                var feeStoreAddress = Addresses.GetShopFeeAddress(arenaData.ChampionshipId, arenaData.Round);
                var tax = product.Price.DivRem(100, out _) * Buy.TaxRate;
                var taxedPrice = product.Price - tax;

                // Receipt
                var receipt = new ProductReceipt(productId, sellerAvatarAddress, buyerAvatarState.address, product.Price,
                    context.BlockIndex);
                states = states
                    .SetState(productAddress, Null.Value)
                    .SetState(productsStateAddress, productsState.Serialize())
                    .SetState(sellerAvatarAddress, sellerAvatarState.SerializeV2())
                    .SetState(sellerAvatarAddress.Derive(LegacyQuestListKey), sellerAvatarState.questList.Serialize())
                    .SetState(ProductReceipt.DeriveAddress(productId), receipt.Serialize())
                    .TransferAsset(context.Signer, feeStoreAddress, tax)
                    .TransferAsset(context.Signer, sellerAgentAddress, taxedPrice);

                if (sellerMigrationRequired)
                {
                    states = states
                        .SetState(sellerAvatarAddress.Derive(LegacyInventoryKey), sellerAvatarState.inventory.Serialize())
                        .SetState(sellerAvatarAddress.Derive(LegacyWorldInformationKey), sellerAvatarState.worldInformation.Serialize());
                }
            }

            if (migrationRequired)
            {
                states = states
                    .SetState(AvatarAddress, buyerAvatarState.SerializeV2())
                    .SetState(AvatarAddress.Derive(LegacyQuestListKey), buyerAvatarState.questList.Serialize())
                    .SetState(AvatarAddress.Derive(LegacyWorldInformationKey), buyerAvatarState.worldInformation.Serialize());
            }


            states = states.SetState(AvatarAddress.Derive(LegacyInventoryKey), buyerAvatarState.inventory.Serialize());
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("BuyProduct Total Executed Time: {Elapsed}", ended - started);
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
            ProductInfos = plainValue["p"].ToList(s => new ProductInfo((List) s));
        }
    }
}
