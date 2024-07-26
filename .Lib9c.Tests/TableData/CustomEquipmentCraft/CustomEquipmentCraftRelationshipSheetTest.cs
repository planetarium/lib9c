namespace Lib9c.Tests.TableData.CustomEquipmentCraft
{
    using System.Linq;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData.CustomEquipmentCraft;
    using Xunit;

    public class CustomEquipmentCraftRelationshipSheetTest
    {
        [Fact]
        public void Set()
        {
            var sheetData =
                @"relationship,cost_multiplier,min_cp,max_cp,weapon_item_id,armor_item_id,belt_item_id,necklace_item_id,ring_item_id
100,1,100,1000,90000001,91000001,92000001,93000001,94000001";
            var sheet = new CustomEquipmentCraftRelationshipSheet();
            sheet.Set(sheetData);

            Assert.Single(sheet.Values);

            var row = sheet.Values.First();

            Assert.Equal(100, row.Relationship);
            Assert.Equal(1, row.CostMultiplier);
            Assert.Equal(100, row.MinCp);
            Assert.Equal(1000, row.MaxCp);
            Assert.Equal(90000001, row.WeaponItemId);
            Assert.Equal(91000001, row.ArmorItemId);
            Assert.Equal(92000001, row.BeltItemId);
            Assert.Equal(93000001, row.NecklaceItemId);
            Assert.Equal(94000001, row.RingItemId);

            Assert.Equal(90000001, row.GetItemId(ItemSubType.Weapon));
            Assert.Equal(91000001, row.GetItemId(ItemSubType.Armor));
            Assert.Equal(92000001, row.GetItemId(ItemSubType.Belt));
            Assert.Equal(93000001, row.GetItemId(ItemSubType.Necklace));
            Assert.Equal(94000001, row.GetItemId(ItemSubType.Ring));
        }
    }
}
