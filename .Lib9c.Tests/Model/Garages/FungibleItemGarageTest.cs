namespace Lib9c.Tests.Model.Garages
{
    using System;
    using Bencodex.Types;
    using Nekoyume.Model.Garages;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class FungibleItemGarageTest
    {
        private static TradableMaterial _tradableMaterial;
        private static TradableMaterial _tradableMaterial2;

        public FungibleItemGarageTest()
        {
            if (!TableSheetsImporter.TryGetCsv(
                    nameof(MaterialItemSheet),
                    out var materialItemSheetCsv))
            {
                throw new Exception("Failed to load MaterialItemSheet.csv");
            }

            var materialItemSheet = new MaterialItemSheet();
            materialItemSheet.Set(materialItemSheetCsv);
            _tradableMaterial =
                ItemFactory.CreateTradableMaterial(materialItemSheet.OrderedList![0]);
            _tradableMaterial2 =
                ItemFactory.CreateTradableMaterial(materialItemSheet.OrderedList![1]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(int.MaxValue)]
        public void Constructor_Success(int count)
        {
            var garage = new FungibleItemGarage(_tradableMaterial, count);
            Assert.Equal(_tradableMaterial, garage.Item);
            Assert.Equal(count, garage.Count);
            AssertSerialization(garage);
            garage = new FungibleItemGarage(_tradableMaterial2, count);
            Assert.Equal(_tradableMaterial2, garage.Item);
            Assert.Equal(count, garage.Count);
            AssertSerialization(garage);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(int.MaxValue)]
        public void Constructor_Failure_With_Null_Item(int count)
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FungibleItemGarage(null!, count));
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        public void Constructor_Failure_With_Negative_Count(int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FungibleItemGarage(_tradableMaterial, count));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FungibleItemGarage(_tradableMaterial2, count));
        }

        [Fact]
        public void Constructor_With_IValue_Failure()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FungibleItemGarage(null));
            Assert.Throws<ArgumentNullException>(() =>
                new FungibleItemGarage(Null.Value));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(0, int.MaxValue)]
        [InlineData(int.MaxValue - 1, 1)]
        [InlineData(int.MaxValue, 0)]
        public void Add_Success(int count1, int count2)
        {
            var garage = new FungibleItemGarage(_tradableMaterial, count1);
            Assert.Equal(count1, garage.Count);
            garage.Add(count2);
            Assert.Equal(count1 + count2, garage.Count);
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(1, -2)]
        [InlineData(1, int.MaxValue)]
        [InlineData(int.MaxValue, 1)]
        public void Add_Failure(int count1, int count2)
        {
            var garage = new FungibleItemGarage(_tradableMaterial, count1);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                garage.Add(count2));
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 1)]
        [InlineData(int.MaxValue, 1)]
        public void TransferTo_Success(int count1, int count2)
        {
            var garage1 = new FungibleItemGarage(_tradableMaterial, count1);
            var garage2 = new FungibleItemGarage(_tradableMaterial, 0);
            garage1.TransferTo(garage2, count2);
            Assert.Equal(count1 - count2, garage1.Count);
            Assert.Equal(count2, garage2.Count);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(1, -1)]
        [InlineData(2, -1)]
        public void TransferTo_Failure_With_Invalid_Count(int count1, int count2)
        {
            var garage1 = new FungibleItemGarage(_tradableMaterial, count1);
            var garage2 = new FungibleItemGarage(_tradableMaterial, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                garage1.TransferTo(garage2, count2));
        }

        [Fact]
        public void TransferTo_Failure_With_Mismatch_Item()
        {
            var garage1 = new FungibleItemGarage(_tradableMaterial, 1);
            var garage2 = new FungibleItemGarage(_tradableMaterial2, 0);
            Assert.Throws<ArgumentException>(() =>
                garage1.TransferTo(garage2, 1));
        }

        private static void AssertSerialization(FungibleItemGarage garage)
        {
            var serialized = garage.Serialize();
            if (garage.Count == 0)
            {
                Assert.Equal(Null.Value, serialized);
                return;
            }

            var deserialized = new FungibleItemGarage(serialized);
            Assert.Equal(garage.Item, deserialized.Item);
            Assert.Equal(garage.Count, deserialized.Count);
            var serialized2 = deserialized.Serialize();
            Assert.Equal(serialized, serialized2);
        }
    }
}
