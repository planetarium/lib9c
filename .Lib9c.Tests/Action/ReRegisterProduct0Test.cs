﻿namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Battle;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Market;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class ReRegisterProduct0Test
    {
        private const long ProductPrice = 100;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Currency _currency;
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly GameConfigState _gameConfigState;
        private IAccountStateDelta _initialState;

        public ReRegisterProduct0Test(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new MockStateDelta();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _goldCurrencyState = new GoldCurrencyState(_currency);

            var shopState = new ShopState();

            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            _gameConfigState = new GameConfigState((Text)_tableSheets.GameConfigSheet.Serialize());
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                _gameConfigState,
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState.avatarAddresses[0] = _avatarAddress;

            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(Addresses.GameConfig, _gameConfigState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize());
        }

        [Theory]
        [InlineData(ItemType.Equipment, "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4", 1, 1, 1, true)]
        [InlineData(ItemType.Costume, "936DA01F-9ABD-4d9d-80C7-02AF85C822A8", 1, 1, 1, true)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 1, 1, 1, true)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 1, 2, true)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 2, 3, true)]
        [InlineData(ItemType.Equipment, "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4", 1, 1, 1, false)]
        [InlineData(ItemType.Costume, "936DA01F-9ABD-4d9d-80C7-02AF85C822A8", 1, 1, 1, false)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 1, 1, 1, false)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 1, 2, false)]
        [InlineData(ItemType.Material, "15396359-04db-68d5-f24a-d89c18665900", 2, 2, 3, false)]
        public void Execute_BackwardCompatibility(
            ItemType itemType,
            string guid,
            int itemCount,
            int inventoryCount,
            int expectedCount,
            bool fromPreviousAction
        )
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            ITradableItem tradableItem;
            var itemId = new Guid(guid);
            var orderId = Guid.NewGuid();
            var updateSellOrderId = Guid.NewGuid();
            ItemSubType itemSubType;
            const long requiredBlockIndex = Order.ExpirationInterval;
            switch (itemType)
            {
                case ItemType.Equipment:
                {
                    var itemUsable = ItemFactory.CreateItemUsable(
                        _tableSheets.EquipmentItemSheet.First,
                        itemId,
                        requiredBlockIndex);
                    tradableItem = itemUsable;
                    itemSubType = itemUsable.ItemSubType;
                    break;
                }

                case ItemType.Costume:
                {
                    var costume = ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet.First, itemId);
                    costume.Update(requiredBlockIndex);
                    tradableItem = costume;
                    itemSubType = costume.ItemSubType;
                    break;
                }

                default:
                {
                    var material = ItemFactory.CreateTradableMaterial(
                        _tableSheets.MaterialItemSheet.OrderedList.First(r => r.ItemSubType == ItemSubType.Hourglass));
                    itemSubType = material.ItemSubType;
                    material.RequiredBlockIndex = requiredBlockIndex;
                    tradableItem = material;
                    break;
                }
            }

            var shardedShopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, orderId);
            var shopState = new ShardedShopStateV2(shardedShopAddress);
            var order = OrderFactory.Create(
                _agentAddress,
                _avatarAddress,
                orderId,
                new FungibleAssetValue(_goldCurrencyState.Currency, 100, 0),
                tradableItem.TradableId,
                requiredBlockIndex,
                itemSubType,
                itemCount
            );

            var orderDigestList = new OrderDigestListState(OrderDigestListState.DeriveAddress(_avatarAddress));
            var prevState = _initialState;

            if (inventoryCount > 1)
            {
                for (int i = 0; i < inventoryCount; i++)
                {
                    // Different RequiredBlockIndex for divide inventory slot.
                    if (tradableItem is ITradableFungibleItem tradableFungibleItem)
                    {
                        var tradable = (TradableMaterial)tradableFungibleItem.Clone();
                        tradable.RequiredBlockIndex = tradableItem.RequiredBlockIndex - i;
                        avatarState.inventory.AddItem(tradable, 2 - i);
                    }
                }
            }
            else
            {
                avatarState.inventory.AddItem((ItemBase)tradableItem, itemCount);
            }

            var sellItem = order.Sell(avatarState);
            var orderDigest = order.Digest(avatarState, _tableSheets.CostumeStatSheet);
            shopState.Add(orderDigest, requiredBlockIndex);
            orderDigestList.Add(orderDigest);

            Assert.True(avatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out _));

            Assert.Equal(inventoryCount, avatarState.inventory.Items.Count);
            Assert.Equal(expectedCount, avatarState.inventory.Items.Sum(i => i.count));

            Assert.Single(shopState.OrderDigestList);
            Assert.Single(orderDigestList.OrderDigestList);

            Assert.Equal(requiredBlockIndex * 2, sellItem.RequiredBlockIndex);

            if (fromPreviousAction)
            {
                prevState = prevState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                prevState = prevState
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(_avatarAddress, avatarState.SerializeV2());
            }

            prevState = prevState
                .SetState(Addresses.GetItemAddress(itemId), sellItem.Serialize())
                .SetState(Order.DeriveAddress(order.OrderId), order.Serialize())
                .SetState(orderDigestList.Address, orderDigestList.Serialize())
                .SetState(shardedShopAddress, shopState.Serialize());

            var currencyState = prevState.GetGoldCurrency();
            var price = new FungibleAssetValue(currencyState, ProductPrice, 0);

            var updateSellInfo = new UpdateSellInfo(
                orderId,
                updateSellOrderId,
                itemId,
                itemSubType,
                price,
                itemCount
            );

            var action = new UpdateSell
            {
                sellerAvatarAddress = _avatarAddress,
                updateSellInfos = new[] { updateSellInfo },
            };

            var expectedState = action.Execute(new ActionContext
            {
                BlockIndex = 101,
                PreviousState = prevState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _agentAddress,
            });

            var updateSellShopAddress = ShardedShopStateV2.DeriveAddress(itemSubType, updateSellOrderId);
            var nextShopState = new ShardedShopStateV2((Dictionary)expectedState.GetState(updateSellShopAddress));
            Assert.Equal(1, nextShopState.OrderDigestList.Count);
            Assert.NotEqual(orderId, nextShopState.OrderDigestList.First().OrderId);
            Assert.Equal(updateSellOrderId, nextShopState.OrderDigestList.First().OrderId);
            Assert.Equal(itemId, nextShopState.OrderDigestList.First().TradableId);
            Assert.Equal(requiredBlockIndex + 101, nextShopState.OrderDigestList.First().ExpiredBlockIndex);

            var productType = tradableItem is TradableMaterial
                ? ProductType.Fungible
                : ProductType.NonFungible;
            var reRegister = new ReRegisterProduct0
            {
                AvatarAddress = _avatarAddress,
                ReRegisterInfos = new List<(IProductInfo, IRegisterInfo)>
                {
                    (
                        new ItemProductInfo
                        {
                            AgentAddress = _agentAddress,
                            AvatarAddress = _avatarAddress,
                            Legacy = true,
                            Price = order.Price,
                            ProductId = orderId,
                            Type = productType,
                        },
                        new RegisterInfo
                        {
                            AvatarAddress = _avatarAddress,
                            ItemCount = itemCount,
                            Price = updateSellInfo.price,
                            TradableId = order.TradableId,
                            Type = productType,
                        }
                    ),
                },
            };

            var actualState = reRegister.Execute(new ActionContext
            {
                BlockIndex = 101,
                PreviousState = prevState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _agentAddress,
            });

            var targetShopState = new ShardedShopStateV2((Dictionary)actualState.GetState(shardedShopAddress));
            var nextOrderDigestListState = new OrderDigestListState((Dictionary)actualState.GetState(orderDigestList.Address));
            Assert.Empty(targetShopState.OrderDigestList);
            Assert.Empty(nextOrderDigestListState.OrderDigestList);
            var productsState =
                new ProductsState(
                    (List)actualState.GetState(ProductsState.DeriveAddress(_avatarAddress)));
            var productId = Assert.Single(productsState.ProductIds);
            var product =
                ProductFactory.DeserializeProduct((List)actualState.GetState(Product.DeriveAddress(productId)));
            Assert.Equal(productId, product.ProductId);
            Assert.Equal(productType, product.Type);
            Assert.Equal(order.Price, product.Price);

            var nextAvatarState = actualState.GetAvatarStateV2(_avatarAddress);
            Assert.Equal(_gameConfigState.ActionPointMax - ReRegisterProduct0.CostAp, nextAvatarState.actionPoint);
        }

        [Fact]
        public void Execute_Throw_ListEmptyException()
        {
            var action = new ReRegisterProduct0
            {
                AvatarAddress = _avatarAddress,
                ReRegisterInfos = new List<(IProductInfo, IRegisterInfo)>(),
            };

            Assert.Throws<ListEmptyException>(() => action.Execute(new ActionContext()));
        }

        [Fact]
        public void Execute_Throw_ArgumentOutOfRangeException()
        {
            var reRegisterInfos = new List<(IProductInfo, IRegisterInfo)>();
            for (int i = 0; i < ReRegisterProduct0.Capacity + 1; i++)
            {
                reRegisterInfos.Add((new ItemProductInfo(), new RegisterInfo()));
            }

            var action = new ReRegisterProduct0
            {
                AvatarAddress = _avatarAddress,
                ReRegisterInfos = reRegisterInfos,
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => action.Execute(new ActionContext()));
        }
    }
}
