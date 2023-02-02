using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("register_product")]
    public class RegisterProduct : GameAction
    {
        public IEnumerable<IRegisterInfo> RegisterInfoList;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            if (RegisterInfoList.Select(r => r.AvatarAddress).Distinct().Count() != 1)
            {
                // 판매자는 동일해야함
                throw new Exception();
            }

            var avatarAddress = RegisterInfoList.First().AvatarAddress;
            if (!states.TryGetAvatarStateV2(context.Signer, avatarAddress, out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException("");
            }

            var productListAddress = ProductList.DeriveAddress(avatarAddress);
            ProductList productList;
            if (states.TryGetState(productListAddress, out List rawProductList))
            {
                productList = new ProductList(rawProductList);
            }
            else
            {
                productList = new ProductList();
                var marketState = states.TryGetState(Addresses.Market, out List rawMarketList)
                    ? new MarketState(rawMarketList)
                    : new MarketState();
                marketState.AvatarAddressList.Add(avatarAddress);
                states = states.SetState(Addresses.Market, marketState.Serialize());
            }
            foreach (var info in RegisterInfoList.OrderBy(r => r.Type).ThenBy(r => r.Price))
            {
                states = Register(context, info, avatarState, productList, states);
            }

            states = states
                .SetState(avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(productListAddress, productList.Serialize());
            if (migrationRequired)
            {
                states = states
                    .SetState(avatarAddress, avatarState.SerializeV2())
                    .SetState(avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize());
            }

            return states;
        }

        public static IAccountStateDelta Register(IActionContext context, IRegisterInfo info, AvatarState avatarState,
            ProductList productList, IAccountStateDelta states)
        {
            switch (info)
            {
                case RegisterInfo registerInfo:
                    switch (info.Type)
                    {
                        case ProductType.Fungible:
                        case ProductType.NonFungible:
                        {
                            var tradableId = registerInfo.TradableId;
                            var itemCount = registerInfo.ItemCount;
                            var type = registerInfo.Type;
                            ITradableItem tradableItem = null;
                            switch (type)
                            {
                                case ProductType.Fungible:
                                    if (avatarState.inventory.TryGetTradableItems(tradableId,
                                            context.BlockIndex, itemCount, out var items))
                                    {
                                        int totalCount = itemCount;
                                        tradableItem = (ITradableItem) items.First().item;
                                        foreach (var inventoryItem in items)
                                        {
                                            int removeCount = Math.Min(totalCount,
                                                inventoryItem.count);
                                            ITradableFungibleItem tradableFungibleItem =
                                                (ITradableFungibleItem) inventoryItem.item;
                                            if (!avatarState.inventory.RemoveTradableItem(
                                                    tradableId,
                                                    tradableFungibleItem.RequiredBlockIndex,
                                                    removeCount))
                                            {
                                                throw new ItemDoesNotExistException("");
                                            }

                                            totalCount -= removeCount;
                                            if (totalCount < 1)
                                            {
                                                break;
                                            }
                                        }

                                        if (totalCount != 0)
                                        {
                                            // 삭제처리 오류
                                            throw new Exception();
                                        }
                                    }

                                    break;
                                case ProductType.NonFungible:
                                    if (avatarState.inventory.TryGetNonFungibleItem(tradableId,
                                            out var item) &&
                                        avatarState.inventory.RemoveNonFungibleItem(tradableId))
                                    {
                                        tradableItem = (ITradableItem) item.item;
                                    }

                                    break;
                                case ProductType.FungibleAssetValue:
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            if (tradableItem is null)
                            {
                                throw new ItemDoesNotExistException("");
                            }

                            Guid productId = context.Random.GenerateRandomGuid();
                            var product = new ItemProduct
                            {
                                ProductId = productId,
                                Price = registerInfo.Price,
                                TradableItem = tradableItem,
                                ItemCount = itemCount,
                                RegisteredBlockIndex = context.BlockIndex,
                                Type = registerInfo.Type,
                                SellerAgentAddress = context.Signer,
                                SellerAvatarAddress = registerInfo.AvatarAddress,
                            };
                            productList.ProductIdList.Add(productId);
                            states = states.SetState(Product.DeriveAddress(productId),
                                product.Serialize());
                            break;
                        }
                        case ProductType.FungibleAssetValue:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                case AssetInfo assetInfo:
                {
                    if (assetInfo.Type == ProductType.FungibleAssetValue)
                    {
                        Guid productId = context.Random.GenerateRandomGuid();
                        Address productAddress = Product.DeriveAddress(productId);
                        FungibleAssetValue asset = assetInfo.Asset;
                        var product = new FavProduct
                        {
                            ProductId = productId,
                            Price = assetInfo.Price,
                            Asset = asset,
                            RegisteredBlockIndex = context.BlockIndex,
                            Type = assetInfo.Type,
                            SellerAgentAddress = context.Signer,
                            SellerAvatarAddress = assetInfo.AvatarAddress,
                        };
                        states = states
                            .TransferAsset(avatarState.address, productAddress, asset)
                            .SetState(productAddress, product.Serialize());
                        productList.ProductIdList.Add(productId);
                        break;
                    }

                    throw new ArgumentOutOfRangeException();
                }
            }

            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["r"] = new List(RegisterInfoList.Select(r => r.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            var serialized = (List) plainValue["r"];
            RegisterInfoList = serialized.Cast<List>()
                .Select(registerList =>
                    registerList[2].ToEnum<ProductType>() == ProductType.FungibleAssetValue
                        ? (IRegisterInfo) new AssetInfo(registerList)
                        : new RegisterInfo(registerList)).ToList();
        }
    }
}
