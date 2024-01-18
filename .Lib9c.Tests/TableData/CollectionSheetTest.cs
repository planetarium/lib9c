namespace Lib9c.Tests.TableData
{
    using Nekoyume.Model.Stat;
    using Nekoyume.TableData;
    using Xunit;

    public class CollectionSheetTest
    {
        [Fact]
        public void Set()
        {
            const string csv = @"id,item_id,count,level,option_count,skill,item_id,count,level,option_count,skillitem_id,count,level,option_count,skillitem_id,count,level,option_count,skillitem_id,count,level,option_count,skill,item_id,count,level,option_count,skill,stat_type,modify_type,modify_value,stat_type,modify_type,modify_value,stat_type,modify_type,modify_value\n
1,1,2,3,4,,,,,,,,,,,,,,,,,,,,,,,,,,,ATK,Add,1,,,,,,\n
2,2,3,4,5,,,,,,,,,,,,,,,,,,,,,,,,,,,ATK,Percentage,1,,,,,,\n";
            var sheet = new CollectionSheet();
            sheet.Set(csv);

            for (int i = 0; i < 1; i++)
            {
                var id = i + 1;
                var row = sheet[id];
                Assert.Equal(id, row.Id);
                Assert.Equal(id, row.Materials.Count);
                Assert.Equal(id, row.StatModifiers.Count);
                for (int j = 0; j < id; j++)
                {
                    var material = row.Materials[j];
                    Assert.Equal(id, material.ItemId);
                    Assert.Equal(id + j + 1, material.Count);
                    Assert.Equal(id + j + 2, material.Level);
                    Assert.Equal(id + j + 3, material.OptionCount);
                    Assert.False(material.SkillContains);

                    var modifier = row.StatModifiers[j];
                    Assert.Equal(StatType.ATK, modifier.StatType);
                    Assert.Equal(id + j, modifier.Value);
                    Assert.Equal(j, (int)modifier.Operation);
                }
            }
        }
    }
}
