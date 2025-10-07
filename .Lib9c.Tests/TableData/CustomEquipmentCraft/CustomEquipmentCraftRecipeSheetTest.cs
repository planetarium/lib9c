namespace Lib9c.Tests.TableData.CustomEquipmentCraft
{
    using System.Linq;
    using Lib9c.Model.Item;
    using Lib9c.TableData.CustomEquipmentCraft;
    using Xunit;

    public class CustomEquipmentCraftRecipeSheetTest
    {
        [Fact]
        public void Set()
        {
            var sheetData = @"id,item_sub_type,scroll_amount,circle_amount,required_block
1,Weapon,1,2,3";
            var sheet = new CustomEquipmentCraftRecipeSheet();
            sheet.Set(sheetData);

            Assert.Single(sheet.Values);

            var row = sheet.Values.First();
            Assert.Equal(ItemSubType.Weapon, row.ItemSubType);
            Assert.Equal(1, row.ScrollAmount);
            Assert.Equal(2, row.CircleAmount);
            Assert.Equal(3, row.RequiredBlock);
        }
    }
}
