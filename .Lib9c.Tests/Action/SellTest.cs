namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class SellTest
    {
        private const long ProductPrice = 100;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Currency _currency;
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;
        private IAccountStateDelta _initialState;

        public SellTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new State();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

            _currency = new Currency("NCG", 2, minters: null);
            var goldCurrencyState = new GoldCurrencyState(_currency);

            var shopState = new ShopState();

            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState.avatarAddresses[0] = _avatarAddress;

            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, goldCurrencyState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize());
        }

        [Theory]
        [InlineData(ItemType.Consumable, true, 2, 1)]
        [InlineData(ItemType.Equipment, true, 2, 1)]
        [InlineData(ItemType.Consumable, false, 0, 1)]
        [InlineData(ItemType.Costume, false, 0, 1)]
        [InlineData(ItemType.Equipment, false, 0, 1)]
        [InlineData(ItemType.Material, true, 1, 2)]
        [InlineData(ItemType.Material, true, 1, 1)]
        [InlineData(ItemType.Material, true, 2, 1)]
        [InlineData(ItemType.Material, true, 3, 2)]
        [InlineData(ItemType.Material, false, 1, 1)]
        public void Execute(
            ItemType itemType,
            bool shopItemExist,
            long blockIndex,
            int itemCount
        )
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);

            ITradableItem tradableItem;
            switch (itemType)
            {
                case ItemType.Consumable:
                    tradableItem = ItemFactory.CreateItemUsable(
                        _tableSheets.ConsumableItemSheet.First,
                        Guid.NewGuid(),
                        0);
                    break;
                case ItemType.Costume:
                    tradableItem = ItemFactory.CreateCostume(
                        _tableSheets.CostumeItemSheet.First,
                        Guid.NewGuid());
                    break;
                case ItemType.Equipment:
                    tradableItem = ItemFactory.CreateItemUsable(
                        _tableSheets.EquipmentItemSheet.First,
                        Guid.NewGuid(),
                        0);
                    break;
                case ItemType.Material:
                    var tradableMaterialRow = _tableSheets.MaterialItemSheet.OrderedList
                        .First(row => row.ItemSubType == ItemSubType.Hourglass);
                    tradableItem = ItemFactory.CreateTradableMaterial(tradableMaterialRow);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null);
            }

            Assert.Equal(0, tradableItem.RequiredBlockIndex);
            avatarState.inventory.AddItem((ItemBase)tradableItem, itemCount);

            var previousStates = _initialState;
            previousStates = previousStates.SetState(_avatarAddress, avatarState.Serialize());
            var currencyState = previousStates.GetGoldCurrency();
            var price = new FungibleAssetValue(currencyState, ProductPrice, 0);
            var orderId = new Guid("6f460c1a755d48e4ad6765d5f519dbc8");
            var existOrderId = new Guid("229e5f8c-fabe-4c04-bab9-45325cfa69a4");
            var orderAddress = Order.DeriveAddress(orderId);
            var shardedShopAddress = ShardedShopStateV3.DeriveAddress(
                tradableItem.ItemSubType,
                tradableItem.TradableId);
            if (shopItemExist)
            {
                tradableItem.RequiredBlockIndex = blockIndex;
                Assert.Equal(blockIndex, tradableItem.RequiredBlockIndex);
                var shardedShopState = new ShardedShopStateV3(shardedShopAddress);
                shardedShopState.OrderList.Add(existOrderId);
                Assert.Single(shardedShopState.OrderList);
                previousStates = previousStates.SetState(
                    shardedShopAddress,
                    shardedShopState.Serialize());
            }
            else
            {
                Assert.Null(previousStates.GetState(shardedShopAddress));
            }

            var sellAction = new Sell
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = tradableItem.TradableId,
                count = itemCount,
                price = price,
                itemSubType = tradableItem.ItemSubType,
                orderId = orderId,
            };
            var nextState = sellAction.Execute(new ActionContext
            {
                BlockIndex = 1,
                PreviousStates = previousStates,
                Rehearsal = false,
                Signer = _agentAddress,
                Random = new TestRandom(),
            });

            const long expiredBlockIndex = Order.ExpirationInterval + 1;

            // Check AvatarState and Inventory
            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.Single(nextAvatarState.inventory.Items);
            Assert.True(nextAvatarState.inventory.TryGetTradableItems(
                tradableItem.TradableId,
                expiredBlockIndex,
                1,
                out var inventoryItems));
            Assert.Single(inventoryItems);
            ITradableItem nextTradableItem = (ITradableItem)inventoryItems.First().item;
            Assert.Equal(expiredBlockIndex, nextTradableItem.RequiredBlockIndex);

            // Check ShardedShopState
            var nextSerializedShardedShopState = nextState.GetState(shardedShopAddress);
            Assert.NotNull(nextSerializedShardedShopState);
            var nextShardedShopState =
                new ShardedShopStateV3((Dictionary)nextSerializedShardedShopState);
            var expectedCount = shopItemExist ? 2 : 1;
            Assert.Equal(expectedCount, nextShardedShopState.OrderList.Count);

            var serializedOrder = nextState.GetState(orderAddress);
            Assert.NotNull(serializedOrder);
            var serializedItem = nextState.GetState(Addresses.GetItemAddress(tradableItem.TradableId));
            Assert.NotNull(serializedItem);

            var order = OrderFactory.Deserialize((Dictionary)serializedOrder);
            ITradableItem orderItem = (ITradableItem)ItemFactory.Deserialize((Dictionary)serializedItem);

            Assert.Equal(price, order.Price);
            Assert.Equal(orderId, order.OrderId);
            Assert.Equal(tradableItem.TradableId, order.TradableId);
            Assert.Equal(expiredBlockIndex, order.ExpiredBlockIndex);
            Assert.Equal(_agentAddress, order.SellerAgentAddress);
            Assert.Equal(_avatarAddress, order.SellerAvatarAddress);
            Assert.Equal(expiredBlockIndex, orderItem.RequiredBlockIndex);

            var mailList = nextAvatarState.mailBox.OfType<OrderExpirationMail>().ToList();
            Assert.Single(mailList);
            var mail = mailList.First();
            Assert.NotNull(mail);
            Assert.Equal(expiredBlockIndex, mail.requiredBlockIndex);
            Assert.Equal(orderId, mail.OrderId);
        }

        [Fact]
        public void Execute_Throw_InvalidPriceException()
        {
            var action = new Sell
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = default,
                count = 1,
                price = -1 * _currency,
                itemSubType = default,
            };

            Assert.Throws<InvalidPriceException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
            }));
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException()
        {
            var action = new Sell
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = default,
                count = 1,
                price = 0 * _currency,
                itemSubType = ItemSubType.Food,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = new State(),
                Signer = _agentAddress,
            }));
        }

        [Fact]
        public void Execute_Throw_NotEnoughClearedStageLevelException()
        {
            var avatarState = new AvatarState(_avatarState)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    0
                ),
            };

            _initialState = _initialState.SetState(_avatarAddress, avatarState.Serialize());

            var action = new Sell
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = default,
                count = 1,
                price = 0 * _currency,
                itemSubType = ItemSubType.Food,
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
            }));
        }

        [Fact]
        public void Execute_Throw_ItemDoesNotExistException()
        {
            var action = new Sell
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = default,
                count = 1,
                price = 0 * _currency,
                itemSubType = ItemSubType.Food,
            };

            Assert.Throws<ItemDoesNotExistException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_Throw_InvalidItemTypeException()
        {
            var equipmentId = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.First,
                equipmentId,
                10);
            _avatarState.inventory.AddItem(equipment);

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new Sell
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = equipmentId,
                count = 1,
                price = 0 * _currency,
                itemSubType = ItemSubType.Food,
            };

            Assert.Throws<InvalidItemTypeException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 11,
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Rehearsal()
        {
            Guid tradableId = Guid.NewGuid();
            Guid orderId = Guid.NewGuid();
            var action = new Sell
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = tradableId,
                count = 1,
                price = _currency * ProductPrice,
                itemSubType = ItemSubType.Weapon,
                orderId = orderId,
            };

            var updatedAddresses = new List<Address>()
            {
                _agentAddress,
                _avatarAddress,
                Addresses.GetItemAddress(tradableId),
                Order.DeriveAddress(orderId),
                ShardedShopStateV3.DeriveAddress(ItemSubType.Weapon, tradableId),
            };

            var state = new State();

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                BlockIndex = 0,
                Rehearsal = true,
            });

            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
        }
    }
}
