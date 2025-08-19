namespace Lib9c.Tests.TableData.Rune
{
    using System.Linq;
    using Nekoyume.TableData;
    using Xunit;

    public class RuneOptionSheetTest
    {
        [Fact]
        public void StatNoneTest()
        {
            const string csvData = @"rune_id,level,total_cp,stat_type_1,value_1,value_type_1,stat_type_2,value_2,value_type_2,stat_type_3,value_3,value_type3,skillId,cooldown,chance,skill_value,skill_stat_ratio,skill_value_type,stat_reference_type,buff_duration
10043,1,364,HP,520,Add,NONE,0,Add,NONE,0,Add,,,,,,,,";
            const string csvData2 = @"rune_id,level,total_cp,stat_type_1,value_1,value_type_1,stat_type_2,value_2,value_type_2,stat_type_3,value_3,value_type3,skillId,cooldown,chance,skill_value,skill_stat_ratio,skill_value_type,stat_reference_type,buff_duration
10043,1,364,HP,520,Add,,,,,,,,,,,,,,";
            var sheet = new RuneOptionSheet();
            sheet.Set(csvData);
            var row = sheet.Values.First();
            var sheetWithoutNone = new RuneOptionSheet();
            sheetWithoutNone.Set(csvData2);
            var rowWithoutNone = sheetWithoutNone.Values.First();
            Assert.Equal(row.RuneId, rowWithoutNone.RuneId);
            var expected = row.LevelOptionMap[1];
            var actual = rowWithoutNone.LevelOptionMap[1];
            Assert.Equal(expected.Cp, actual.Cp);
            var (expectedStat, expectedType) = Assert.Single(expected.Stats);
            var (actualStat, actualType) = Assert.Single(actual.Stats);
            Assert.Equal(expectedStat, actualStat);
            Assert.Equal(expectedType, actualType);
        }
    }
}
