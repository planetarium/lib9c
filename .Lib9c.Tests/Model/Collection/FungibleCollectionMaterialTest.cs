namespace Lib9c.Tests.Model.Collection
{
    using System;
    using System.Linq;
    using Lib9c.Action;
    using Lib9c.Model.Collection;
    using Lib9c.Model.Item;
    using Lib9c.Tests.Action;
    using Xunit;

    public class FungibleCollectionMaterialTest
    {
        private readonly TableSheets _tableSheets = new (TableSheetsImporter.ImportSheets());

        [Theory]
        [InlineData(ItemType.Consumable)]
        [InlineData(ItemType.Material)]
        public void BurnMaterial(ItemType itemType)
        {
            var inventory = new Inventory();
            var row = _tableSheets.ItemSheet.Values.First(r => r.ItemType == itemType);
            var item = ItemFactory.CreateItem(row, new TestRandom());
            inventory.AddItem(item);
            Assert.Single(inventory.Items);
            var fungibleMaterial = new FungibleCollectionMaterial
            {
                ItemId = row.Id,
                ItemCount = 1,
            };
            fungibleMaterial.BurnMaterial(row, inventory, 0L);
            Assert.Empty(inventory.Items);
        }

        [Theory]
        [InlineData(0L, 1L, false)]
        [InlineData(1L, 0L, true)]
        public void BurnMaterial_Consumable_ItemDoesNotExistException(long requiredBlockIndex, long blockIndex, bool add)
        {
            var inventory = new Inventory();
            var row = _tableSheets.ItemSheet.Values.First(r => r.ItemType == ItemType.Consumable);
            var item = ItemFactory.CreateItemUsable(row, Guid.NewGuid(), requiredBlockIndex);
            if (add)
            {
                inventory.AddItem(item);
                Assert.Single(inventory.Items);
            }
            else
            {
                Assert.Empty(inventory.Items);
            }

            var fungibleMaterial = new FungibleCollectionMaterial
            {
                ItemId = row.Id,
                ItemCount = 1,
            };
            Assert.Throws<ItemDoesNotExistException>(() => fungibleMaterial.BurnMaterial(row, inventory, blockIndex));
        }

        [Fact]
        public void BurnMaterial_Material_ItemDoesNotExistException()
        {
            var inventory = new Inventory();
            var row = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemType == ItemType.Material);
            var tradableMaterial = ItemFactory.CreateTradableMaterial(row);
            tradableMaterial.RequiredBlockIndex = 1L;
            inventory.AddItem(tradableMaterial);
            Assert.Single(inventory.Items);

            var fungibleMaterial = new FungibleCollectionMaterial
            {
                ItemId = row.Id,
                ItemCount = 1,
            };

            // required block index
            Assert.Throws<ItemDoesNotExistException>(() => fungibleMaterial.BurnMaterial(row, inventory, 0L));

            // insufficient count
            fungibleMaterial.ItemCount = 2;
            Assert.Throws<ItemDoesNotExistException>(() => fungibleMaterial.BurnMaterial(row, inventory, 1L));

            var material = ItemFactory.CreateMaterial(row);
            inventory.AddItem(material);
            Assert.Equal(2, inventory.Items.Count);

            // required block index
            Assert.Throws<ItemDoesNotExistException>(() => fungibleMaterial.BurnMaterial(row, inventory, 0L));
        }

        [Theory]
        [InlineData(ItemType.Costume)]
        [InlineData(ItemType.Equipment)]
        public void BurnMaterial_InvalidItemTypeException(ItemType itemType)
        {
            var inventory = new Inventory();
            var row = _tableSheets.ItemSheet.Values.First(r => r.ItemType == itemType);
            var item = ItemFactory.CreateItem(row, new TestRandom());
            inventory.AddItem(item);
            Assert.Single(inventory.Items);

            var fungibleMaterial = new FungibleCollectionMaterial
            {
                ItemId = row.Id,
                ItemCount = 1,
            };
            Assert.Throws<InvalidItemTypeException>(() => fungibleMaterial.BurnMaterial(row, inventory, 0L));
        }
    }
}
