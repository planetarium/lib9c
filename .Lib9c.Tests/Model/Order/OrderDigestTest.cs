namespace Lib9c.Tests.Model.Order
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Lib9c.Model.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Xunit;

    public class OrderDigestTest
    {
        private readonly Currency _currency;

        public OrderDigestTest()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
        }

        [Fact]
        public void Serialize()
        {
            var digest = new OrderDigest(
                default,
                1,
                2,
                Guid.NewGuid(),
                Guid.NewGuid(),
                new FungibleAssetValue(_currency, 100, 0),
                1,
                3,
                100,
                1
            );
            var serialized = (Dictionary)digest.Serialize();
            Assert.Equal(digest, new OrderDigest(serialized));
        }

        [Fact]
        public void Serialize_DotNet_Api()
        {
            var digest = new OrderDigest(
                default,
                1,
                2,
                Guid.NewGuid(),
                Guid.NewGuid(),
                new FungibleAssetValue(_currency, 100, 0),
                1,
                3,
                100,
                1
            );

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, digest);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (OrderDigest)formatter.Deserialize(ms);
            Assert.Equal(digest, deserialized);
        }

        [Theory]
        [InlineData(123456789L)]
        [InlineData(987654321L)]
        [InlineData(0L)]
        [InlineData(-123456L)]
        public void Constructor_WithLongCombatPoint_ShouldSetCombatPointCorrectly(long combatPoint)
        {
            // Arrange & Act
            var orderId = Guid.NewGuid();
            var tradableId = Guid.NewGuid();
            var price = new FungibleAssetValue(_currency, 1000, 0);
            int level = 50;
            int itemId = 100;
            int itemCount = 1;
            var sellerAgentAddress = new Address("0x1234567890123456789012345678901234567890");

            var orderDigest = new OrderDigest(
                sellerAgentAddress,
                1L,
                2L,
                orderId,
                tradableId,
                price,
                combatPoint,
                level,
                itemId,
                itemCount);

            // Assert
            Assert.Equal(combatPoint, orderDigest.CombatPoint);
        }

        [Theory]
        [InlineData(555666777L)]
        [InlineData(999999999999L)]
        [InlineData(0L)]
        [InlineData(-123456L)]
        public void SerializeAndDeserialize_RoundTrip_ShouldPreserveCombatPoint(long originalCombatPoint)
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var tradableId = Guid.NewGuid();
            var price = new FungibleAssetValue(_currency, 1000, 0);
            int level = 50;
            int itemId = 100;
            int itemCount = 1;
            var sellerAgentAddress = new Address("0x1234567890123456789012345678901234567890");

            var originalOrderDigest = new OrderDigest(
                sellerAgentAddress,
                1L,
                2L,
                orderId,
                tradableId,
                price,
                originalCombatPoint,
                level,
                itemId,
                itemCount);

            // Act
            var serialized = originalOrderDigest.Serialize();
            var deserializedOrderDigest = new OrderDigest((Dictionary)serialized);

            // Assert
            Assert.Equal(originalCombatPoint, deserializedOrderDigest.CombatPoint);
        }

        [Fact]
        public void SerializeAndDeserialize_WithMaxIntCombatPoint_ShouldHandleCorrectly()
        {
            // Arrange
            int maxIntValue = int.MaxValue;
            var orderId = Guid.NewGuid();
            var tradableId = Guid.NewGuid();
            var price = new FungibleAssetValue(_currency, 1000, 0);
            int level = 50;
            int itemId = 100;
            int itemCount = 1;
            var sellerAgentAddress = new Address("0x1234567890123456789012345678901234567890");

            var orderDigest = new OrderDigest(
                sellerAgentAddress,
                1L,
                2L,
                orderId,
                tradableId,
                price,
                maxIntValue,
                level,
                itemId,
                itemCount);

            // Act
            var serialized = orderDigest.Serialize();
            var deserializedOrderDigest = new OrderDigest((Dictionary)serialized);

            // Assert
            Assert.Equal((long)maxIntValue, deserializedOrderDigest.CombatPoint);
        }

        [Theory]
        [InlineData(789012L)]
        [InlineData(0L)]
        [InlineData(-123456L)]
        public void Serialize_WithLongCombatPoint_ShouldSerializeCorrectly(long combatPoint)
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var tradableId = Guid.NewGuid();
            var price = new FungibleAssetValue(_currency, 1000, 0);
            int level = 50;
            int itemId = 100;
            int itemCount = 1;
            var sellerAgentAddress = new Address("0x1234567890123456789012345678901234567890");

            var orderDigest = new OrderDigest(
                sellerAgentAddress,
                1L,
                2L,
                orderId,
                tradableId,
                price,
                combatPoint,
                level,
                itemId,
                itemCount);

            // Act
            var serialized = orderDigest.Serialize();

            // Assert
            var dict = Assert.IsType<Dictionary>(serialized);
            Assert.Equal(combatPoint, dict["cp"].ToLong());
        }
    }
}
