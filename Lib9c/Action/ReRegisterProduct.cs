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
    [ActionType("re_register_item")]
    public class ReRegisterProduct : GameAction
    {
        public Address AvatarAddress;
        public List<(ProductInfo, IRegisterInfo)> ReRegisterInfoList;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            if (ReRegisterInfoList.Any(tuple =>
                    tuple.Item1.AvatarAddress != AvatarAddress ||
                    tuple.Item2.AvatarAddress != AvatarAddress))
            {
                throw new Exception();
            }

            if (!states.TryGetAvatarStateV2(context.Signer, AvatarAddress, out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException("");
            }

            var productListAddress = ProductList.DeriveAddress(AvatarAddress);
            var productList = new ProductList((List) states.GetState(productListAddress));
            foreach (var (productInfo, info) in ReRegisterInfoList.OrderBy(tuple => tuple.Item2.Type).ThenBy(tuple => tuple.Item2.Price))
            {
                var productId = productInfo.ProductId;
                if (!productList.ProductIdList.Contains(productId))
                {
                    throw new Exception();
                }

                productList.ProductIdList.Remove(productId);

                var productAddress = Product.DeriveAddress(productId);
                var product = Product.Deserialize((List) states.GetState(productAddress));
                switch (product)
                {
                    case FavProduct favProduct:
                        states = states.TransferAsset(productAddress, AvatarAddress,
                            favProduct.Asset);
                        break;
                    case ItemProduct itemProduct:
                    {
                        switch (itemProduct.TradableItem)
                        {
                            case Costume costume:
                                avatarState.UpdateFromAddCostume(costume, true);
                                break;
                            case ItemUsable itemUsable:
                                avatarState.UpdateFromAddItem(itemUsable, true);
                                break;
                            case TradableMaterial tradableMaterial:
                            {
                                avatarState.UpdateFromAddItem(tradableMaterial,
                                    itemProduct.ItemCount, true);
                                break;
                            }
                        }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(product));
                }

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

                                Guid newProductId = context.Random.GenerateRandomGuid();
                                var newProduct = new ItemProduct
                                {
                                    ProductId = newProductId,
                                    Price = registerInfo.Price,
                                    TradableItem = tradableItem,
                                    ItemCount = itemCount,
                                };
                                productList.ProductIdList.Add(newProductId);
                                states = states
                                    .SetState(productAddress, Null.Value)
                                    .SetState(Product.DeriveAddress(newProductId),
                                        newProduct.Serialize());
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
                            Guid newProductId = context.Random.GenerateRandomGuid();
                            Address newProductAddress = Product.DeriveAddress(newProductId);
                            FungibleAssetValue asset = assetInfo.Asset;
                            var newProduct = new FavProduct
                            {
                                ProductId = newProductId,
                                Price = assetInfo.Price,
                                Asset = asset,
                            };
                            states = states
                                .TransferAsset(AvatarAddress, newProductAddress, asset)
                                .SetState(productAddress, Null.Value)
                                .SetState(newProductAddress, newProduct.Serialize());
                            productList.ProductIdList.Add(newProductId);
                            break;
                        }

                        throw new ArgumentOutOfRangeException();
                    }
                }
            }

            states = states
                .SetState(AvatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(productListAddress, productList.Serialize());

            if (migrationRequired)
            {
                states = states
                    .SetState(AvatarAddress, avatarState.SerializeV2())
                    .SetState(AvatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize())
                    .SetState(AvatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize());
            }
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["r"] = new List(ReRegisterInfoList.Select(tuple =>
                    List.Empty.Add(tuple.Item1.Serialize()).Add(tuple.Item2.Serialize()))),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            ReRegisterInfoList = new List<(ProductInfo, IRegisterInfo)>();
            var serialized = (List) plainValue["r"];
            foreach (var value in serialized)
            {
                var innerList = (List) value;
                var registerList = (List) innerList[1];
                IRegisterInfo info =
                    registerList[2].ToEnum<ProductType>() == ProductType.FungibleAssetValue
                        ? (IRegisterInfo) new AssetInfo(registerList)
                        : new RegisterInfo(registerList);
                ReRegisterInfoList.Add((new ProductInfo((List)innerList[0]), info));
            }
        }
    }
}
