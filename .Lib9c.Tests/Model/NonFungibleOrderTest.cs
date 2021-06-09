namespace Lib9c.Tests.Model
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Libplanet.Assets;
    using Nekoyume;
    using Xunit;

    public class NonFungibleOrderTest
    {
        private readonly TableSheets _tableSheets;

        public NonFungibleOrderTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Serialize()
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
                1
            );
            Assert.Equal(1, order.StartedBlockIndex);
            Assert.Equal(currency * 10, order.Price);
            Assert.Equal(Order.OrderType.NonFungible, order.Type);
            Assert.Equal(Addresses.Admin, order.SellerAgentAddress);
            Assert.Equal(Addresses.Blacksmith, order.SellerAvatarAddress);
            Assert.Equal(orderId, order.OrderId);
            Assert.Equal(itemId, order.TradableId);
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
                1
            );
            Assert.Equal(1, order.StartedBlockIndex);
            Assert.Equal(currency * 10, order.Price);
            Assert.Equal(Order.OrderType.NonFungible, order.Type);
            Assert.Equal(Addresses.Admin, order.SellerAgentAddress);
            Assert.Equal(Addresses.Blacksmith, order.SellerAvatarAddress);
            Assert.Equal(orderId, order.OrderId);
            Assert.Equal(itemId, order.TradableId);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, order);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (NonFungibleOrder)formatter.Deserialize(ms);

            Assert.Equal(order, deserialized);
            Assert.Equal(order.Serialize(), deserialized.Serialize());
        }
    }
}
