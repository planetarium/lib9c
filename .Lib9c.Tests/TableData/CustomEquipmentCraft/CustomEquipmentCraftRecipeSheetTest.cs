namespace Lib9c.Tests.TableData.CustomEquipmentCraft
{
    using System.Linq;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData.CustomEquipmentCraft;
    using Xunit;

    public class CustomEquipmentCraftRecipeSheetTest
    {
        [Fact]
        public void Set()
        {
            var sheetData = @"id,item_sub_type,drawing_amount,drawing_tool_amount,required_block
1,Weapon,1,2,3";
            var sheet = new CustomEquipmentCraftRecipeSheet();
            sheet.Set(sheetData);

            Assert.Single(sheet.Values);

            var row = sheet.Values.First();
            Assert.Equal(ItemSubType.Weapon, row.ItemSubType);
            Assert.Equal(1, row.DrawingAmount);
            Assert.Equal(2, row.DrawingToolAmount);
            Assert.Equal(3, row.RequiredBlock);
        }
    }
}
