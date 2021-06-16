namespace Lib9c.Tests.Model.Order
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Xunit;

    public class NonFungibleOrderTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Currency _currency;
        private readonly AvatarState _avatarState;

        public NonFungibleOrderTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _currency = new Currency("NCG", 2, minter: null);
            _avatarState = new AvatarState(
                Addresses.Blacksmith,
                Addresses.Admin,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default,
                "name"
            );
        }

        [Fact]
        public void Serialize()
        {
            Guid orderId = new Guid("6d460c1a-755d-48e4-ad67-65d5f519dbc8");
            Guid itemId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                Addresses.Admin,
                Addresses.Blacksmith,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                itemId,
                1,
                ItemSubType.Weapon
            );

            Assert.Equal(1, order.StartedBlockIndex);
            Assert.Equal(_currency * 10, order.Price);
            Assert.Equal(Order.OrderType.NonFungible, order.Type);
            Assert.Equal(Addresses.Admin, order.SellerAgentAddress);
            Assert.Equal(Addresses.Blacksmith, order.SellerAvatarAddress);
            Assert.Equal(orderId, order.OrderId);
            Assert.Equal(itemId, order.TradableId);
            Assert.Equal(ItemSubType.Weapon, order.ItemSubType);

            Dictionary serialized = (Dictionary)order.Serialize();

            Assert.Equal(order, new NonFungibleOrder(serialized));
        }

        [Fact]
        public void Serialize_DotNet_Api()
        {
            Guid orderId = new Guid("6d460c1a-755d-48e4-ad67-65d5f519dbc8");
            Guid itemId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            Currency currency = new Currency("NCG", 2, minter: null);
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                Addresses.Admin,
                Addresses.Blacksmith,
                orderId,
                new FungibleAssetValue(currency, 10, 0),
                itemId,
                1,
                ItemSubType.Weapon
            );

            Assert.Equal(1, order.StartedBlockIndex);
            Assert.Equal(currency * 10, order.Price);
            Assert.Equal(Order.OrderType.NonFungible, order.Type);
            Assert.Equal(Addresses.Admin, order.SellerAgentAddress);
            Assert.Equal(Addresses.Blacksmith, order.SellerAvatarAddress);
            Assert.Equal(orderId, order.OrderId);
            Assert.Equal(itemId, order.TradableId);
            Assert.Equal(ItemSubType.Weapon, order.ItemSubType);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, order);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (NonFungibleOrder)formatter.Deserialize(ms);

            Assert.Equal(order, deserialized);
            Assert.Equal(order.Serialize(), deserialized.Serialize());
        }

        [Theory]
        [MemberData(nameof(ValidateMemberData))]
        public void Validate(
            int count,
            int requiredBlockIndex,
            Address agentAddress,
            Address avatarAddress,
            bool add,
            ItemSubType itemSubType,
            ItemSubType orderItemSubType,
            Type exc
        )
        {
            var row = _tableSheets.ItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType);
            ItemBase item = ItemFactory.CreateItem(row, new TestRandom());
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            tradableItem.RequiredBlockIndex = requiredBlockIndex;
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                agentAddress,
                avatarAddress,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
                1,
                orderItemSubType
            );
            if (add)
            {
                _avatarState.inventory.AddNonFungibleItem(item);
            }

            if (exc is null)
            {
                order.Validate(_avatarState, 1);
            }
            else
            {
                Assert.Throws(exc, () => order.Validate(_avatarState, count));
            }
        }

        [Theory]
        [InlineData(ItemSubType.Weapon, true, null)]
        [InlineData(ItemSubType.Weapon, false, typeof(ItemDoesNotExistException))]
        [InlineData(ItemSubType.FullCostume, true, null)]
        [InlineData(ItemSubType.Food, false, typeof(ItemDoesNotExistException))]
        public void Sell(ItemSubType itemSubType, bool add, Type exc)
        {
            var row = _tableSheets.ItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType);
            ItemBase item = ItemFactory.CreateItem(row, new TestRandom());
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                _avatarState.agentAddress,
                _avatarState.address,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
                1,
                itemSubType
            );
            if (add)
            {
                _avatarState.inventory.AddNonFungibleItem(item);
            }

            if (item is Equipment equipment)
            {
                equipment.Equip();
            }

            Assert.Equal(add, _avatarState.inventory.TryGetTradableItems(tradableItem.TradableId, order.StartedBlockIndex, 1, out _));

            if (add)
            {
                ITradableItem result = order.Sell(_avatarState);
                Assert.Equal(order.ExpiredBlockIndex, result.RequiredBlockIndex);
                if (result is Equipment equipmentResult)
                {
                    Assert.False(equipmentResult.equipped);
                }
            }
            else
            {
                Assert.Throws(exc, () => order.Sell(_avatarState));
            }

            Assert.False(_avatarState.inventory.TryGetTradableItems(tradableItem.TradableId, order.StartedBlockIndex, 1, out _));
            Assert.Equal(add, _avatarState.inventory.TryGetTradableItems(tradableItem.TradableId, order.ExpiredBlockIndex, 1, out _));
        }

        [Theory]
        [InlineData(ItemSubType.Weapon, true, null)]
        [InlineData(ItemSubType.Weapon, false, typeof(ItemDoesNotExistException))]
        [InlineData(ItemSubType.FullCostume, true, null)]
        [InlineData(ItemSubType.Food, false, typeof(ItemDoesNotExistException))]
        public void Digest(ItemSubType itemSubType, bool add, Type exc)
        {
            var row = _tableSheets.ItemSheet.OrderedList.First(r => r.ItemSubType == itemSubType);
            ItemBase item = ItemFactory.CreateItem(row, new TestRandom());
            Guid orderId = new Guid("15396359-04db-68d5-f24a-d89c18665900");
            ITradableItem tradableItem = (ITradableItem)item;
            tradableItem.RequiredBlockIndex = 1;
            NonFungibleOrder order = OrderFactory.CreateNonFungibleOrder(
                _avatarState.agentAddress,
                _avatarState.address,
                orderId,
                new FungibleAssetValue(_currency, 10, 0),
                tradableItem.TradableId,
                1,
                itemSubType
            );

            if (add)
            {
                _avatarState.inventory.AddNonFungibleItem(item);

                int cp = CPHelper.GetCP(tradableItem, _tableSheets.CostumeStatSheet);
                Assert.True(cp > 0);
                OrderDigest digest = order.Digest(_avatarState, _tableSheets.CostumeStatSheet);

                Assert.Equal(orderId, digest.OrderId);
                Assert.Equal(order.StartedBlockIndex, digest.StartedBlockIndex);
                Assert.Equal(order.ExpiredBlockIndex, digest.ExpiredBlockIndex);
                Assert.Equal(order.Price, digest.Price);
                Assert.Equal(item.Id, digest.ItemId);
                Assert.Equal(cp, digest.CombatPoint);
                Assert.Equal(0, digest.Level);
            }
            else
            {
                Assert.Throws(exc, () => order.Digest(_avatarState, _tableSheets.CostumeStatSheet));
            }
        }

#pragma warning disable SA1204
        public static IEnumerable<object[]> ValidateMemberData() => new List<object[]>
        {
            new object[]
            {
                1,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                true,
                ItemSubType.Weapon,
                ItemSubType.Weapon,
                null,
            },
            new object[]
            {
                1,
                0,
                default,
                default,
                false,
                ItemSubType.Weapon,
                ItemSubType.Weapon,
                typeof(InvalidAddressException),
            },
            new object[]
            {
                0,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                false,
                ItemSubType.Food,
                ItemSubType.Food,
                typeof(InvalidItemCountException),
            },
            new object[]
            {
                -1,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                false,
                ItemSubType.Food,
                ItemSubType.Food,
                typeof(InvalidItemCountException),
            },
            new object[]
            {
                2,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                false,
                ItemSubType.FullCostume,
                ItemSubType.FullCostume,
                typeof(InvalidItemCountException),
            },
            new object[]
            {
                1,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                false,
                ItemSubType.Weapon,
                ItemSubType.Weapon,
                typeof(ItemDoesNotExistException),
            },
            new object[]
            {
                1,
                0,
                Addresses.Admin,
                Addresses.Blacksmith,
                true,
                ItemSubType.Weapon,
                ItemSubType.Food,
                typeof(InvalidItemTypeException),
            },
            new object[]
            {
                1,
                100,
                Addresses.Admin,
                Addresses.Blacksmith,
                true,
                ItemSubType.Weapon,
                ItemSubType.Weapon,
                typeof(RequiredBlockIndexException),
            },
        };
#pragma warning restore SA1204
    }
}
