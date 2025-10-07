namespace Lib9c.Tests.Model.Collection
{
    using System;
    using System.Linq;
    using Lib9c.Action;
    using Lib9c.Model.Collection;
    using Lib9c.Model.Item;
    using Lib9c.TableData;
    using Lib9c.Tests.Action;
    using Xunit;

    public class NonFungibleCollectionMaterialTest
    {
        private readonly TableSheets _tableSheets = new (TableSheetsImporter.ImportSheets());

        [Theory]
        [InlineData(ItemType.Equipment)]
        [InlineData(ItemType.Costume)]
        public void BurnMaterial(ItemType itemType)
        {
            var inventory = new Inventory();
            var row = _tableSheets.ItemSheet.Values.First(r => r.ItemType == itemType);
            var materialInfo = new CollectionSheet.RequiredMaterial
            {
                ItemId = row.Id,
                Count = 1,
                Level = 0,
                SkillContains = false,
            };
            var item = ItemFactory.CreateItem(row, new TestRandom());
            var nonfungibleId = ((INonFungibleItem)item).NonFungibleId;
            inventory.AddItem(item);
            Assert.Single(inventory.Items);
            var nonFungibleCollectionMaterial = new NonFungibleCollectionMaterial
            {
                ItemId = row.Id,
                ItemCount = 1,
                NonFungibleId = nonfungibleId,
            };
            nonFungibleCollectionMaterial.BurnMaterial(row, inventory, materialInfo, 0L);
            Assert.Empty(inventory.Items);
        }

        [Theory]
        [InlineData(ItemType.Equipment, "6856AE42-A820-4041-92B0-5D7BAA52F2AA", 100L, 10L)]
        [InlineData(ItemType.Costume, "701BA698-CCB9-4FC7-B88F-7CB8C707D135", 100L, 99L)]
        [InlineData(ItemType.Equipment, "6f460c1a-755d-48e4-ad67-65d5f519dbc8", 11L, 10L)]
        [InlineData(ItemType.Costume, "6f460c1a-755d-48e4-ad67-65d5f519dbc8", 1L, 0L)]
        public void BurnMaterial_ItemDoesNotExistException(ItemType itemType, Guid nonFungibleId, long requiredBlockIndex, long blockIndex)
        {
            var inventory = new Inventory();
            var row = _tableSheets.ItemSheet.Values.First(r => r.ItemType == itemType);
            var materialInfo = new CollectionSheet.RequiredMaterial
            {
                ItemId = row.Id,
                Count = 1,
                Level = 1,
                SkillContains = false,
            };
            var item = ItemFactory.CreateItem(row, new TestRandom());
            var tradableItem = (ITradableItem)item;
            tradableItem.RequiredBlockIndex = requiredBlockIndex;
            inventory.AddItem(item);
            Assert.Single(inventory.Items);
            var fungibleMaterial = new NonFungibleCollectionMaterial
            {
                ItemId = row.Id,
                ItemCount = 1,
                NonFungibleId = nonFungibleId,
            };
            Assert.Throws<ItemDoesNotExistException>(() => fungibleMaterial.BurnMaterial(row, inventory, materialInfo, blockIndex));
        }

        [Theory]
        [InlineData(ItemType.Material)]
        [InlineData(ItemType.Consumable)]
        public void BurnMaterial_InvalidItemTypeException(ItemType itemType)
        {
            var inventory = new Inventory();
            var row = _tableSheets.ItemSheet.Values.First(r => r.ItemType == itemType);
            var item = ItemFactory.CreateItem(row, new TestRandom());
            inventory.AddItem(item);
            Assert.Single(inventory.Items);

            var materialInfo = new CollectionSheet.RequiredMaterial
            {
                ItemId = row.Id,
                Count = 1,
                Level = 0,
                SkillContains = false,
            };
            var nonFungibleMaterial = new NonFungibleCollectionMaterial
            {
                ItemId = row.Id,
                ItemCount = 1,
            };
            Assert.Throws<InvalidItemTypeException>(() => nonFungibleMaterial.BurnMaterial(row, inventory, materialInfo, 0L));
        }
    }
}
