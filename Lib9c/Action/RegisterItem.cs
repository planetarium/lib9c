using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("register_item")]
    public class RegisterItem : GameAction
    {
        public IEnumerable<RegisterInfo> RegisterInfos;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            if (RegisterInfos.Select(r => r.AvatarAddress).Distinct().Count() != 1)
            {
                // 판매자는 동일해야함
                throw new Exception();
            }

            if (RegisterInfos.Any(r => r.Type == ProductType.FungibleAssetValue))
            {
                // 에셋은 별도 처리
                throw new Exception();
            }

            var avatarAddress = RegisterInfos.First().AvatarAddress;
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
            foreach (var registerInfo in RegisterInfos.OrderBy(r => r.TradableId).ThenBy(r => r.Price))
            {
                var tradableId = registerInfo.TradableId;
                var itemCount = registerInfo.ItemCount;
                var type = registerInfo.Type;
                ITradableItem tradableItem = null;
                switch (type)
                {
                    case ProductType.Fungible:
                        if (avatarState.inventory.TryGetTradableItems(tradableId, context.BlockIndex, itemCount, out var items))
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

                Guid productId = context.Random.GenerateRandomGuid();
                var product = new Product
                {
                    ProductId = productId,
                    Price = registerInfo.Price,
                    TradableItem = tradableItem,
                    ItemCount = itemCount,
                };
                productList.ProductIdList.Add(productId);
                states = states.SetState(Product.DeriveAddress(productId), product.Serialize());
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

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["r"] = new List(RegisterInfos.Select(r => r.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            RegisterInfos = plainValue["r"].ToList(s => new RegisterInfo((List) s));
        }
    }
}
