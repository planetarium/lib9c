using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("buy5")]
    public class Buy : GameAction
    {
        public const int TaxRate = 8;
        public const int ERROR_CODE_FAILED_LOADING_STATE = 1;
        public const int ERROR_CODE_ITEM_DOES_NOT_EXIST = 2;
        public const int ERROR_CODE_SHOPITEM_EXPIRED = 3;
        public const int ERROR_CODE_INSUFFICIENT_BALANCE = 4;

        public Address buyerAvatarAddress;
        public IEnumerable<PurchaseInfo> purchaseInfos;
        public BuyerMultipleResult buyerMultipleResult;
        public SellerMultipleResult sellerMultipleResult;

        [Serializable]
        public class BuyerResult : AttachmentActionResult
        {
            public ShopItem shopItem;
            public Guid id;

            protected override string TypeId => "buy.buyerResult";

            public BuyerResult()
            {
            }

            public BuyerResult(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                shopItem = new ShopItem((Bencodex.Types.Dictionary) serialized["shopItem"]);
                id = serialized["id"].ToGuid();
            }

            public override IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "shopItem"] = shopItem.Serialize(),
                    [(Text) "id"] = id.Serialize(),
                }.Union((Bencodex.Types.Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }

        [Serializable]
        public class SellerResult : AttachmentActionResult
        {
            public ShopItem shopItem;
            public Guid id;
            public FungibleAssetValue gold;

            protected override string TypeId => "buy.sellerResult";

            public SellerResult()
            {
            }

            public SellerResult(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                shopItem = new ShopItem((Bencodex.Types.Dictionary) serialized["shopItem"]);
                id = serialized["id"].ToGuid();
                gold = serialized["gold"].ToFungibleAssetValue();
            }

            public override IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "shopItem"] = shopItem.Serialize(),
                    [(Text) "id"] = id.Serialize(),
                    [(Text) "gold"] = gold.Serialize(),
                }.Union((Bencodex.Types.Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }

        [Serializable]
        public class PurchaseInfo
        {
            public Guid productId;
            public Address sellerAgentAddress;
            public Address sellerAvatarAddress;
            public ItemSubType itemSubType;

            public PurchaseInfo(Guid productId, Address sellerAgentAddress, Address sellerAvatarAddress, ItemSubType itemSubType)
            {
                this.productId = productId;
                this.sellerAgentAddress = sellerAgentAddress;
                this.sellerAvatarAddress = sellerAvatarAddress;
                this.itemSubType = itemSubType;
            }

            public PurchaseInfo(Bencodex.Types.Dictionary serialized)
            {
                productId = serialized[ProductIdKey].ToGuid();
                sellerAvatarAddress = serialized[SellerAvatarAddressKey].ToAddress();
                sellerAgentAddress = serialized[SellerAgentAddressKey].ToAddress();
                itemSubType = serialized[ItemSubTypeKey].ToEnum<ItemSubType>();
            }

            public IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) ProductIdKey] = productId.Serialize(),
                    [(Text) SellerAvatarAddressKey] = sellerAvatarAddress.Serialize(),
                    [(Text) SellerAgentAddressKey] = sellerAgentAddress.Serialize(),
                    [(Text) ItemSubTypeKey] = itemSubType.Serialize(),
                });
#pragma warning restore LAA1002
        }

        [Serializable]
        public class PurchaseResult : BuyerResult
        {
            public int errorCode = 0;
            public Guid productId;

            public PurchaseResult(Guid productId)
            {
                this.productId = productId;
            }

            public PurchaseResult(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                errorCode = serialized[ErrorCodeKey].ToInteger();
                productId = serialized[ProductIdKey].ToGuid();
            }

            public override IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) ErrorCodeKey] = errorCode.Serialize(),
                    [(Text) ProductIdKey] = productId.Serialize(),
                }.Union((Bencodex.Types.Dictionary)base.Serialize()));
#pragma warning restore LAA1002
        }

        [Serializable]
        public class BuyerMultipleResult
        {
            public IEnumerable<PurchaseResult> purchaseResults;

            public BuyerMultipleResult()
            {
            }

            public BuyerMultipleResult(Bencodex.Types.Dictionary serialized)
            {
                purchaseResults = serialized[PurchaseResultsKey].ToList(StateExtensions.ToPurchaseResult);
            }

            public IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) PurchaseResultsKey] = purchaseResults
                        .OrderBy(i => i)
                        .Select(g => g.Serialize()).Serialize()
                });
#pragma warning restore LAA1002
        }

        [Serializable]
        public class SellerMultipleResult
        {
            public IEnumerable<SellerResult> sellerResults;

            public SellerMultipleResult()
            {
            }

            public SellerMultipleResult(Bencodex.Types.Dictionary serialized)
            {
                sellerResults = serialized[SellerResultsKey].ToList(StateExtensions.ToSellerResult);
            }

            public IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) SellerResultsKey] = sellerResults
                        .OrderBy(i => i)
                        .Select(g => g.Serialize()).Serialize()
                });
#pragma warning restore LAA1002
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["buyerAvatarAddress"] = buyerAvatarAddress.Serialize(),
            [PurchaseInfosKey] = purchaseInfos
                .OrderBy(p => p.productId)
                .Select(p => p.Serialize())
                .Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            buyerAvatarAddress = plainValue["buyerAvatarAddress"].ToAddress();
            purchaseInfos = plainValue[PurchaseInfosKey].ToList(StateExtensions.ToPurchaseInfo);
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                foreach (var purchaseInfo in purchaseInfos)
                {
                    Address shardedShopAddress =
                        ShardedShopState.DeriveAddress(purchaseInfo.itemSubType, purchaseInfo.productId);
                    states = states
                        .SetState(shardedShopAddress, MarkChanged)
                        .SetState(purchaseInfo.sellerAvatarAddress, MarkChanged)
                        .MarkBalanceChanged(
                            GoldCurrencyMock,
                            ctx.Signer,
                            purchaseInfo.sellerAgentAddress,
                            GoldCurrencyState.Address);
                }

                return states
                    .SetState(buyerAvatarAddress, MarkChanged)
                    .SetState(ctx.Signer, MarkChanged)
                    .SetState(Addresses.Shop, MarkChanged);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, buyerAvatarAddress);

            if (purchaseInfos.Select(p => p.sellerAgentAddress).Contains(ctx.Signer))
            {
                throw new InvalidAddressException($"{addressesHex}Aborted as the signer is the seller.");
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Buy exec started", addressesHex);

            if (!states.TryGetAvatarState(ctx.Signer, buyerAvatarAddress, out var buyerAvatarState))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the buyer was failed to load.");
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Get Buyer AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            if (!buyerAvatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                buyerAvatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(addressesHex, GameConfig.RequireClearedStageLevel.ActionsInShop, current);
            }

            List<PurchaseResult> purchaseResults = new List<PurchaseResult>();
            List<SellerResult> sellerResults = new List<SellerResult>();
            MaterialItemSheet materialSheet = states.GetSheet<MaterialItemSheet>();
            buyerMultipleResult = new BuyerMultipleResult();
            sellerMultipleResult = new SellerMultipleResult();

            foreach (var purchaseInfo in purchaseInfos)
            {
                PurchaseResult purchaseResult = new PurchaseResult(purchaseInfo.productId);
                Address shardedShopAddress =
                    ShardedShopState.DeriveAddress(purchaseInfo.itemSubType, purchaseInfo.productId);
                Address sellerAgentAddress = purchaseInfo.sellerAgentAddress;
                Address sellerAvatarAddress = purchaseInfo.sellerAvatarAddress;

                purchaseResults.Add(purchaseResult);
                if (!states.TryGetState(shardedShopAddress, out Bencodex.Types.Dictionary shopStateDict))
                {
                    purchaseResult.errorCode = ERROR_CODE_FAILED_LOADING_STATE;
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

                // 상점에서 구매할 아이템을 찾는다.
                List products = (List)shopStateDict[ProductsKey];
                IValue productIdSerialized = purchaseInfo.productId.Serialize();
                Dictionary productSerialized = products
                    .Select(p => (Dictionary) p)
                    .FirstOrDefault(p => p[ProductIdKey].Equals(productIdSerialized));
                if (productSerialized.Equals(Dictionary.Empty))
                {
                    purchaseResult.errorCode = ERROR_CODE_ITEM_DOES_NOT_EXIST;
                    continue;
                }

                ShopItem shopItem = new ShopItem(productSerialized);
                if (!shopItem.SellerAgentAddress.Equals(sellerAgentAddress))
                {
                    purchaseResult.errorCode = ERROR_CODE_ITEM_DOES_NOT_EXIST;
                    continue;
                }
                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Get Item: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();

                if (0 < shopItem.ExpiredBlockIndex && shopItem.ExpiredBlockIndex < context.BlockIndex)
                {
                    purchaseResult.errorCode = ERROR_CODE_SHOPITEM_EXPIRED;
                    continue;
                }

                if (!states.TryGetAvatarState(sellerAgentAddress, sellerAvatarAddress, out var sellerAvatarState))
                {
                    purchaseResult.errorCode = ERROR_CODE_FAILED_LOADING_STATE;
                    continue;
                }
                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Get Seller AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();

                // 돈은 있냐?
                FungibleAssetValue buyerBalance = states.GetBalance(context.Signer, states.GetGoldCurrency());
                if (buyerBalance < shopItem.Price)
                {
                    purchaseResult.errorCode = ERROR_CODE_INSUFFICIENT_BALANCE;
                    continue;
                }

                var tax = shopItem.Price.DivRem(100, out _) * TaxRate;
                var taxedPrice = shopItem.Price - tax;

                // 세금을 송금한다.
                states = states.TransferAsset(
                    context.Signer,
                    GoldCurrencyState.Address,
                    tax);

                // 구매자의 돈을 판매자에게 송금한다.
                states = states.TransferAsset(
                    context.Signer,
                    sellerAgentAddress,
                    taxedPrice
                );


                products = (List) products.Remove(productSerialized);
                shopStateDict = shopStateDict.SetItem(ProductsKey, new List<IValue>(products));

                INonFungibleItem nonFungibleItem = (INonFungibleItem) shopItem.ItemUsable ?? shopItem.Costume;
                if (!sellerAvatarState.inventory.RemoveNonFungibleItem(nonFungibleItem))
                {
                    // Backward compatibility.
                    IValue rawShop = states.GetState(Addresses.Shop);
                    if (!(rawShop is null))
                    {
                        Dictionary legacyShopDict = (Dictionary) rawShop;
                        Dictionary legacyProducts = (Dictionary) legacyShopDict[LegacyProductsKey];
                        IKey productKey = (IKey) purchaseInfo.productId.Serialize();
                        // SoldOut
                        if (!legacyProducts.ContainsKey(productKey))
                        {
                            purchaseResult.errorCode = ERROR_CODE_ITEM_DOES_NOT_EXIST;
                            continue;
                        }

                        legacyProducts = (Dictionary) legacyProducts.Remove(productKey);
                        legacyShopDict = legacyShopDict.SetItem(LegacyProductsKey, legacyProducts);
                        states = states.SetState(Addresses.Shop, legacyShopDict);
                    }
                }

                nonFungibleItem.Update(context.BlockIndex);


                // 구매자, 판매자에게 결과 메일 전송
                purchaseResult.shopItem = shopItem;
                purchaseResult.itemUsable = shopItem.ItemUsable;
                purchaseResult.costume = shopItem.Costume;
                var buyerMail = new BuyerMail(purchaseResult, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(), ctx.BlockIndex);
                purchaseResult.id = buyerMail.id;

                var sr = new SellerResult
                {
                    shopItem = shopItem,
                    itemUsable = shopItem.ItemUsable,
                    costume = shopItem.Costume,
                    gold = taxedPrice
                };
                var sellerMail = new SellerMail(sr, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(),
                    ctx.BlockIndex);
                sr.id = sellerMail.id;
                sellerResults.Add(sr);

                buyerAvatarState.UpdateV4(buyerMail, context.BlockIndex);
                if (purchaseResult.itemUsable != null)
                {
                    buyerAvatarState.UpdateFromAddItem(purchaseResult.itemUsable, false);
                }

                if (purchaseResult.costume != null)
                {
                    buyerAvatarState.UpdateFromAddCostume(purchaseResult.costume, false);
                }
                sellerAvatarState.UpdateV4(sellerMail, context.BlockIndex);

                // 퀘스트 업데이트
                buyerAvatarState.questList.UpdateTradeQuest(TradeType.Buy, shopItem.Price);
                sellerAvatarState.questList.UpdateTradeQuest(TradeType.Sell, shopItem.Price);

                sellerAvatarState.updatedAt = ctx.BlockIndex;
                sellerAvatarState.blockIndex = ctx.BlockIndex;
                buyerAvatarState.UpdateQuestRewards(materialSheet);
                sellerAvatarState.UpdateQuestRewards(materialSheet);

                states = states.SetState(sellerAvatarAddress, sellerAvatarState.Serialize());
                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Set Seller AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
                sw.Restart();
                states = states.SetState(shardedShopAddress, shopStateDict);
                sw.Stop();
                Log.Verbose("{AddressesHex}Buy Set ShopState: {Elapsed}", addressesHex, sw.Elapsed);
            }

            buyerMultipleResult.purchaseResults = purchaseResults;
            sellerMultipleResult.sellerResults = sellerResults;

            buyerAvatarState.updatedAt = ctx.BlockIndex;
            buyerAvatarState.blockIndex = ctx.BlockIndex;

            states = states.SetState(buyerAvatarAddress, buyerAvatarState.Serialize());
            sw.Stop();
            Log.Verbose("{AddressesHex}Buy Set Buyer AvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            var ended = DateTimeOffset.UtcNow;
            Log.Verbose("{AddressesHex}Buy Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return states;
        }
    }
}
