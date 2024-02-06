namespace Lib9c.Tests.TableData
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Model.Collection;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stat;
    using Nekoyume.TableData;
    using Xunit;

    public class CollectionSheetTest
    {
        [Fact]
        public void Set()
        {
            const string csv = @"id,item_id,count,level,skill,item_id,count,level,skill,item_id,count,level,skill,item_id,count,level,skill,item_id,count,level,skill,item_id,count,level,option_count,skill,stat_type,modify_type,modify_value,stat_type,modify_type,modify_value,stat_type,modify_type,modify_value\n
1,1,2,3,,,,,,,,,,,,,,,,,,,,,,ATK,Add,1,,,,,,\n
2,2,3,4,,,,,,,,,,,,,,,,,,,,,,ATK,Percentage,1,,,,,,\n";
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
                    Assert.False(material.SkillContains);

                    var modifier = row.StatModifiers[j];
                    Assert.Equal(StatType.ATK, modifier.StatType);
                    Assert.Equal(id + j, modifier.Value);
                    Assert.Equal(j, (int)modifier.Operation);
                }
            }
        }

        [Theory]
        [InlineData(ItemType.Equipment)]
        [InlineData(ItemType.Costume)]
        public void Validate(ItemType itemType)
        {
            var row = new TableSheets(TableSheetsImporter.ImportSheets()).ItemSheet.Values.First(r => r.ItemType == itemType);
            var item = ItemFactory.CreateItem(row, new TestRandom());
            var materialInfo = new CollectionSheet.CollectionMaterial
            {
                ItemId = row.Id,
                Count = 1,
                Level = 0,
                SkillContains = false,
            };
            Assert.True(materialInfo.Validate((INonFungibleItem)item));
            if (item is Equipment equipment)
            {
                materialInfo.SkillContains = true;
                Assert.False(materialInfo.Validate(equipment));
            }
        }
    }
}
