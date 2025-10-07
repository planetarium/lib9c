namespace Lib9c.Tests.TableData.WorldAndStage
{
    using Lib9c.TableData.WorldAndStage;
    using Xunit;

    public class WorldUnlockSheetTest
    {
        [Theory]
        [InlineData(
            @"id,world_id,stage_id,world_id_to_unlock
1,1,50,2",
            0)]
        [InlineData(
            @"id,world_id,stage_id,world_id_to_unlock,required_crystal
1,1,50,2,500",
            500)]
        public void Set(string csv, int expected)
        {
            var sheet = new WorldUnlockSheet();
            sheet.Set(csv);

            Assert.Single(sheet);
            var row = sheet.First;
            Assert.Equal(expected, row!.CRYSTAL);
        }
    }
}
