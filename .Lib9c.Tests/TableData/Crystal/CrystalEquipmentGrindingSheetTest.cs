namespace Lib9c.Tests.TableData.Crystal
{
    using System.Linq;
    using Lib9c.TableData.Crystal;
    using Xunit;

    public class CrystalEquipmentGrindingSheetTest
    {
        [Fact]
        public void Set()
        {
            var csv = @"id,enchant_base_id,gain_crystal,reward_material1,count1,reward_material2,count2
10100000,10100000,10,400000,1,500000,2";
            var sheet = new CrystalEquipmentGrindingSheet();
            sheet.Set(csv);
            var row = sheet.First().Value;

            Assert.Equal(10100000, row.ItemId);
            Assert.Equal(10100000, row.EnchantBaseId);
            Assert.Equal(10, row.CRYSTAL);
            Assert.Collection(
                row.RewardMaterials,
                element =>
                {
                    Assert.Equal(400000, element.materialId);
                    Assert.Equal(1, element.count);
                },
                element =>
                {
                    Assert.Equal(500000, element.materialId);
                    Assert.Equal(2, element.count);
                }
            );
        }
    }
}
