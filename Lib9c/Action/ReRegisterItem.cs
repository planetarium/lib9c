using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("re_register_item")]
    public class ReRegisterItem : GameAction
    {
        public Address AvatarAddress;
        public List<(ProductInfo, RegisterInfo)> ReRegisterInfoList;
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
            foreach (var (productInfo, registerInfo) in ReRegisterInfoList)
            {
                var productId = productInfo.ProductId;
                if (!productList.ProductIdList.Contains(productId))
                {
                    throw new Exception();
                }

                productList.ProductIdList.Remove(productId);

                var productAddress = Product.DeriveAddress(productId);
                var product = new Product((List) states.GetState(productAddress));
                switch (product.TradableItem)
                {
                    case Costume costume:
                        avatarState.UpdateFromAddCostume(costume, true);
                        break;
                    case ItemUsable itemUsable:
                        avatarState.UpdateFromAddItem(itemUsable, true);
                        break;
                    case TradableMaterial tradableMaterial:
                    {
                        avatarState.UpdateFromAddItem(tradableMaterial, product.ItemCount, true);
                        break;
                    }
                }

                var tradableId = registerInfo.TradableId;
                var itemCount = registerInfo.ItemCount;
                var type = registerInfo.Type;
                ITradableItem tradableItem = null;
                switch (type)
                {
                    case ProductType.Fungible:
                        if (avatarState.inventory.TryGetTradableItems(tradableId, product.TradableItem.RequiredBlockIndex, itemCount, out var items))
                        {
                            int totalCount = itemCount;
                            tradableItem = (ITradableItem)items.First().item;
                            foreach (var inventoryItem in items)
                            {
                                int removeCount = Math.Min(totalCount, inventoryItem.count);
                                ITradableFungibleItem tradableFungibleItem = (ITradableFungibleItem) inventoryItem.item;
                                if (!avatarState.inventory.RemoveTradableItem(tradableId,
                                        tradableFungibleItem.RequiredBlockIndex, removeCount))
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
                                out var item) && avatarState.inventory.RemoveNonFungibleItem(tradableId))
                        {
                            tradableItem = (ITradableItem)item.item;
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
                var newProduct = new Product
                {
                    ProductId = newProductId,
                    Price = registerInfo.Price,
                    TradableItem = tradableItem,
                    ItemCount = itemCount,
                };
                productList.ProductIdList.Add(newProductId);
                states = states
                    .SetState(productAddress, Null.Value)
                    .SetState(Product.DeriveAddress(newProductId), newProduct.Serialize());
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
            ReRegisterInfoList = new List<(ProductInfo, RegisterInfo)>();
            var serialized = (List) plainValue["r"];
            foreach (var value in serialized)
            {
                var innerList = (List) value;
                ReRegisterInfoList.Add((new ProductInfo((List)innerList[0]), new RegisterInfo((List)innerList[1])));
            }
        }
    }
}
