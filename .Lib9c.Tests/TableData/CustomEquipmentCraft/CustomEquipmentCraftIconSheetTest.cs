namespace Lib9c.Tests.TableData.CustomEquipmentCraft
{
    using System.Linq;
    using Lib9c.Model.Item;
    using Lib9c.TableData.CustomEquipmentCraft;
    using Xunit;

    public class CustomEquipmentCraftIconSheetTest
    {
        [Theory]
        [InlineData(
            @"id,item_sub_type,icon_id,required_relationship,random_only,ratio
1,Weapon,11000001,0,false,90",
            ItemSubType.Weapon,
            11000001,
            0,
            false,
            90)]
        [InlineData(
            @"id,item_sub_type,icon_id,required_relationship,random_only,ratio
1,Weapon,11000002,0,true,190",
            ItemSubType.Weapon,
            11000002,
            0,
            true,
            190)]
        [InlineData(
            @"id,item_sub_type,icon_id,required_relationship,random_only,ratio
1,Armor,11000003,100,false,90",
            ItemSubType.Armor,
            11000003,
            100,
            false,
            90)]
        public void Set(
            string sheetData,
            ItemSubType expectedSubType,
            int expectedIconId,
            int expectedRelationship,
            bool expectedRandom,
            int expectedRatio
        )
        {
            var sheet = new CustomEquipmentCraftIconSheet();
            sheet.Set(sheetData);
            Assert.Single(sheet.Values);

            var row = sheet.Values.First();
            Assert.Equal(expectedSubType, row.ItemSubType);
            Assert.Equal(expectedRelationship, row.RequiredRelationship);
            Assert.Equal(expectedIconId, row.IconId);
            Assert.Equal(expectedRandom, row.RandomOnly);
            Assert.Equal(expectedRatio, row.Ratio);
        }
    }
}
