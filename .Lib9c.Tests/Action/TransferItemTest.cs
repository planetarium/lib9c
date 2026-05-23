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
    using static Lib9c.SerializeKeys;

    public class TransferItemTest
    {
        private const long ProductPrice = 100;

        private readonly Address _agentAddress;
        private readonly Address _agent2Address;
        private readonly Address _avatarAddress;
        private readonly Address _avatar2Address;
        private readonly Currency _currency;
        private readonly AvatarState _avatarState;
        private readonly AvatarState _avatar2State;
        private readonly TableSheets _tableSheets;
        private readonly GoldCurrencyState _goldCurrencyState;
        private IAccountStateDelta _initialState;

        public TransferItemTest(ITestOutputHelper outputHelper)
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
            _goldCurrencyState = new GoldCurrencyState(_currency);

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

            _agent2Address = new PrivateKey().ToAddress();
            var agent2State = new AgentState(_agent2Address);
            _avatar2Address = new PrivateKey().ToAddress();
            var rankingMap2Address = new PrivateKey().ToAddress();
            _avatar2State = new AvatarState(
                _avatar2Address,
                _agent2Address,
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
            agent2State.avatarAddresses[0] = _avatar2Address;

            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetState(Addresses.Shop, shopState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize())
                .SetState(_agent2Address, agent2State.Serialize())
                .SetState(_avatar2Address, _avatar2State.Serialize())
                .MintAsset(_agentAddress, _goldCurrencyState.Currency * 10000);
        }

        [Theory]
        [InlineData(ItemType.Consumable, 1, true)]
        [InlineData(ItemType.Costume, 1, false)]
        [InlineData(ItemType.Equipment, 1, true)]
        [InlineData(ItemType.Material, 1, false)]
        public void Execute(
            ItemType itemType,
            int itemCount,
            bool backward
        )
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var avatar2State = _initialState.GetAvatarState(_avatar2Address);

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
            if (backward)
            {
                previousStates = previousStates.SetState(_avatarAddress, avatarState.Serialize())
                    .SetState(_avatar2Address, avatar2State.Serialize());
            }
            else
            {
                previousStates = previousStates
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(_avatar2Address.Derive(LegacyInventoryKey), avatar2State.inventory.Serialize())
                    .SetState(_avatar2Address.Derive(LegacyWorldInformationKey), avatar2State.worldInformation.Serialize())
                    .SetState(_avatar2Address.Derive(LegacyQuestListKey), avatar2State.questList.Serialize())
                    .SetState(_avatar2Address, avatar2State.SerializeV2());
            }

            var currencyState = previousStates.GetGoldCurrency();
            var price = new FungibleAssetValue(currencyState, ProductPrice, 0);
            var orderId = new Guid("6f460c1a755d48e4ad6765d5f519dbc8");
            var orderAddress = Order.DeriveAddress(orderId);
            var shardedShopAddress = ShardedShopStateV2.DeriveAddress(
                tradableItem.ItemSubType,
                orderId);
            long blockIndex = 1;
            Assert.Null(previousStates.GetState(shardedShopAddress));

            var transferItemAction = new TransferItem
            {
                SenderAvatarAddress = _avatarAddress,
                ItemId = tradableItem.TradableId,
                RecipientAvatarAddress = _avatar2Address,
            };
            var nextState = transferItemAction.Execute(
                new ActionContext
                {
                    BlockIndex = blockIndex,
                    PreviousStates = previousStates,
                    Rehearsal = false,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                });

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            var nextAvatar2State = nextState.GetAvatarStateV2(_avatar2Address);
            Assert.Single(nextAvatar2State.inventory.Items);

            Assert.Empty(nextAvatarState.inventory.Items);
            //Assert.True(nextAvatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out var inventoryItem));
            Assert.False(nextAvatarState.inventory.TryGetTradableItems(tradableItem.TradableId, blockIndex, itemCount, out _));
            Assert.False(nextAvatarState.inventory.TryGetTradableItems(tradableItem.TradableId, blockIndex, itemCount, out _));
            Assert.True(nextAvatar2State.inventory.TryGetTradableItems(tradableItem.TradableId, blockIndex, itemCount, out _));
            Assert.True(nextAvatar2State.inventory.TryGetTradableItems(tradableItem.TradableId, blockIndex, itemCount, out _));
            //ITradableItem nextTradableItem = (ITradableItem)inventoryItem.item;
            //Assert.Equal(expiredBlockIndex, nextTradableItem.RequiredBlockIndex);

            // Check ShardedShopState
            //var nextSerializedShardedShopState = nextState.GetState(shardedShopAddress);
            //Assert.NotNull(nextSerializedShardedShopState);
            //var nextShardedShopState =
            //    new ShardedShopStateV2((Dictionary)nextSerializedShardedShopState);
            //Assert.Single(nextShardedShopState.OrderDigestList);
            //var orderDigest = nextShardedShopState.OrderDigestList.First(o => o.OrderId.Equals(orderId));
            //Assert.Equal(price, orderDigest.Price);
            //Assert.Equal(blockIndex, orderDigest.StartedBlockIndex);
            //Assert.Equal(expiredBlockIndex, orderDigest.ExpiredBlockIndex);
            //Assert.Equal(((ItemBase)tradableItem).Id, orderDigest.ItemId);
            //Assert.Equal(tradableItem.TradableId, orderDigest.TradableId);

            //var serializedOrder = nextState.GetState(orderAddress);
            //Assert.NotNull(serializedOrder);
            //var serializedItem = nextState.GetState(Addresses.GetItemAddress(tradableItem.TradableId));
            //Assert.NotNull(serializedItem);

            //var order = OrderFactory.Deserialize((Dictionary)serializedOrder);
            //ITradableItem orderItem = (ITradableItem)ItemFactory.Deserialize((Dictionary)serializedItem);

            //Assert.Equal(price, order.Price);
            //Assert.Equal(orderId, order.OrderId);
            //Assert.Equal(tradableItem.TradableId, order.TradableId);
            //Assert.Equal(blockIndex, order.StartedBlockIndex);
            //Assert.Equal(expiredBlockIndex, order.ExpiredBlockIndex);
            //Assert.Equal(_agentAddress, order.SellerAgentAddress);
            //Assert.Equal(_avatarAddress, order.SellerAvatarAddress);
            //Assert.Equal(expiredBlockIndex, orderItem.RequiredBlockIndex);

            //var receiptDict = nextState.GetState(OrderDigestListState.DeriveAddress(_avatarAddress));
            //Assert.NotNull(receiptDict);
            //var orderDigestList = new OrderDigestListState((Dictionary)receiptDict);
            //Assert.Single(orderDigestList.OrderDigestList);
            //OrderDigest orderDigest2 = orderDigestList.OrderDigestList.First();
            //Assert.Equal(orderDigest, orderDigest2);
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException()
        {
            var action = new TransferItem
            {
                SenderAvatarAddress = _avatarAddress,
                ItemId = default,
                RecipientAvatarAddress = _avatar2Address,
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

            var action = new TransferItem
            {
                SenderAvatarAddress = _avatarAddress,
                ItemId = default,
                RecipientAvatarAddress = _avatar2Address,
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Signer = _agentAddress,
            }));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_Throw_ItemDoesNotExistException(bool isLock)
        {
            var tradableId = Guid.NewGuid();
            if (isLock)
            {
                var tradableItem = ItemFactory.CreateItemUsable(
                    _tableSheets.EquipmentItemSheet.First,
                    tradableId,
                    0);
                var orderLock = new OrderLock(Guid.NewGuid());
                _avatarState.inventory.AddItem(tradableItem, 1, orderLock);
                Assert.True(_avatarState.inventory.TryGetLockedItem(orderLock, out _));
                _initialState = _initialState.SetState(
                    _avatarAddress.Derive(LegacyInventoryKey),
                    _avatarState.inventory.Serialize()
                );
            }

            var action = new TransferItem
            {
                SenderAvatarAddress = _avatarAddress,
                ItemId = tradableId,
                RecipientAvatarAddress = _avatar2Address,
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
        public void Rehearsal()
        {
            Guid tradableId = Guid.NewGuid();
            Guid orderId = Guid.NewGuid();
            var action = new TransferItem
            {
                SenderAvatarAddress = _avatarAddress,
                ItemId = tradableId,
                ItemCount = 1,
                RecipientAvatarAddress = _avatar2Address,
            };

            var updatedAddresses = new List<Address>()
            {
                _agentAddress,
                _avatarAddress.Derive(LegacyInventoryKey),
                _avatarAddress.Derive(LegacyWorldInformationKey),
                _avatarAddress.Derive(LegacyQuestListKey),
                Addresses.GetItemAddress(tradableId),
                _avatar2Address.Derive(LegacyInventoryKey),
                _avatar2Address.Derive(LegacyWorldInformationKey),
                _avatar2Address.Derive(LegacyQuestListKey),
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
