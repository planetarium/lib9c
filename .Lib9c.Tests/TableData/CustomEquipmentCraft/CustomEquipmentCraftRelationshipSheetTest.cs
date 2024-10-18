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
                @"relationship,cost_multiplier,required_block_multiplier,min_cp_1,max_cp_1,ratio_1,min_cp_2,max_cp_2,ratio_2,min_cp_3,max_cp_3,ratio_3,min_cp_4,max_cp_4,ratio_4,min_cp_5,max_cp_5,ratio_5,min_cp_6,max_cp_6,ratio_6,min_cp_7,max_cp_7,ratio_7,min_cp_8,max_cp_8,ratio_8,min_cp_9,max_cp_9,ratio_9,min_cp_10,max_cp_10,ratio_10,weapon_item_id,armor_item_id,belt_item_id,necklace_item_id,ring_item_id,gold_amount,material_1_id,material_1_amount,material_2_id,material_2_amount
100,1,1,100,200,10,200,300,10,300,400,10,400,500,10,500,600,10,600,700,10,700,800,10,800,900,10,900,1000,10,,,,90000001,91000001,92000001,93000001,94000001,1,600201,2,600203,4";
            var sheet = new CustomEquipmentCraftRelationshipSheet();
            sheet.Set(sheetData);

            Assert.Single(sheet.Values);

            var row = sheet.Values.First();

            var minCpList = new[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, };
            Assert.Equal(100, row.Relationship);
            Assert.Equal(1, row.CostMultiplier);
            Assert.Equal(1, row.RequiredBlockMultiplier);
            Assert.Equal(9, row.CpGroups.Count);
            foreach (var group in row.CpGroups)
            {
                Assert.Equal(10, group.Ratio);
                Assert.Contains(group.MinCp, minCpList);
                Assert.Equal(group.MinCp + 100, group.MaxCp);
            }

            Assert.Equal(90000001, row.WeaponItemId);
            Assert.Equal(91000001, row.ArmorItemId);
            Assert.Equal(92000001, row.BeltItemId);
            Assert.Equal(93000001, row.NecklaceItemId);
            Assert.Equal(94000001, row.RingItemId);

            Assert.Equal(1, row.GoldAmount);
            Assert.Equal(2, row.MaterialCosts.Count);
            Assert.Equal(600201, row.MaterialCosts[0].ItemId);
            Assert.Equal(2, row.MaterialCosts[0].Amount);
            Assert.Equal(600203, row.MaterialCosts[1].ItemId);
            Assert.Equal(4, row.MaterialCosts[1].Amount);

            Assert.Equal(90000001, row.GetItemId(ItemSubType.Weapon));
            Assert.Equal(91000001, row.GetItemId(ItemSubType.Armor));
            Assert.Equal(92000001, row.GetItemId(ItemSubType.Belt));
            Assert.Equal(93000001, row.GetItemId(ItemSubType.Necklace));
            Assert.Equal(94000001, row.GetItemId(ItemSubType.Ring));
        }
    }
}
