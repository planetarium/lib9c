namespace Lib9c.Tests.Model.Collection
{
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Action;
    using Nekoyume.Model.Collection;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
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
            var materialInfo = new CollectionSheet.CollectionMaterial
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
            nonFungibleCollectionMaterial.BurnMaterial(row, inventory, materialInfo);
            Assert.Empty(inventory.Items);
        }

        [Theory]
        [InlineData(ItemType.Equipment)]
        [InlineData(ItemType.Costume)]
        public void BurnMaterial_ItemDoesNotExistException(ItemType itemType)
        {
            var inventory = new Inventory();
            var row = _tableSheets.ItemSheet.Values.First(r => r.ItemType == itemType);
            var materialInfo = new CollectionSheet.CollectionMaterial
            {
                ItemId = row.Id,
                Count = 1,
                Level = 1,
                SkillContains = false,
            };
            var item = ItemFactory.CreateItem(row, new TestRandom());
            inventory.AddItem(item);
            Assert.Single(inventory.Items);
            var fungibleMaterial = new NonFungibleCollectionMaterial
            {
                ItemId = row.Id,
                ItemCount = 1,
            };
            Assert.Throws<ItemDoesNotExistException>(() => fungibleMaterial.BurnMaterial(row, inventory, materialInfo));
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

            var materialInfo = new CollectionSheet.CollectionMaterial
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
            Assert.Throws<InvalidItemTypeException>(() => nonFungibleMaterial.BurnMaterial(row, inventory, materialInfo));
        }
    }
}
