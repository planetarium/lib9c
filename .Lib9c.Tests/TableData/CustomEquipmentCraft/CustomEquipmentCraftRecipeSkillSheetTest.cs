namespace Lib9c.Tests.TableData.CustomEquipmentCraft
{
    using System.Linq;
    using Lib9c.Model.Item;
    using Lib9c.TableData.CustomEquipmentCraft;
    using Xunit;

    public class CustomEquipmentCraftRecipeSkillSheetTest
    {
        [Fact]
        public void Set()
        {
            var sheetData = @"id,item_sub_type,skill_id,ratio
1,Weapon,10101010,50";
            var sheet = new CustomEquipmentCraftRecipeSkillSheet();
            sheet.Set(sheetData);

            Assert.Single(sheet.Values);

            var row = sheet.Values.First();
            Assert.Equal(ItemSubType.Weapon, row.ItemSubType);
            Assert.Equal(10101010, row.ItemOptionId);
            Assert.Equal(50, row.Ratio);
        }
    }
}
