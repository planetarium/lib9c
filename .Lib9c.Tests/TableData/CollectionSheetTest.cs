namespace Lib9c.Tests.TableData
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Model.Collection;
    using Lib9c.Model.Item;
    using Lib9c.Model.Skill;
    using Lib9c.Model.Stat;
    using Lib9c.TableData;
    using Lib9c.Tests.Action;
    using Xunit;

    public class CollectionSheetTest
    {
        private readonly TableSheets _tableSheets = new (TableSheetsImporter.ImportSheets());

        [Fact]
        public void Set()
        {
            const string csv =
                @"id,item_id,count,level,skill,item_id,count,level,skill,item_id,count,level,skill,item_id,count,level,skill,item_id,count,level,skill,item_id,count,level,option_count,skill,stat_type,modify_type,modify_value,stat_type,modify_type,modify_value,stat_type,modify_type,modify_value\n
1,1,2,3,,,,,,,,,,,,,,,,,,,,,,ATK,Add,1,,,,,,\n
2,2,3,4,,,,,,,,,,,,,,,,,,,,,,ATK,Percentage,1,,,,,,\n";
            var sheet = new CollectionSheet();
            sheet.Set(csv);

            for (var i = 0; i < 1; i++)
            {
                var id = i + 1;
                var row = sheet[id];
                Assert.Equal(id, row.Id);
                Assert.Equal(id, row.Materials.Count);
                Assert.Equal(id, row.StatModifiers.Count);
                for (var j = 0; j < id; j++)
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
        [InlineData(true, 0, 0, false)]
        [InlineData(true, 1, 0, true)]
        [InlineData(true, 1, 1, true)]
        [InlineData(true, 0, 1, true)]
        [InlineData(false, 0, 0, true)]
        [InlineData(false, 1, 0, false)]
        [InlineData(false, 1, 1, false)]
        [InlineData(false, 0, 1, false)]
        public void Validate_Equipment(bool skillContains, int skillCount, int buffSkillCount, bool expected)
        {
            var row = _tableSheets.ItemSheet.Values.First(r => r.ItemType == ItemType.Equipment);
            var item = (Equipment)ItemFactory.CreateItem(row, new TestRandom());
            for (var i = 0; i < skillCount; i++)
            {
                var skillRow = _tableSheets.SkillSheet.Values.First();
                var skill = SkillFactory.Get(skillRow, 0, 0, 0, StatType.NONE);
                item.Skills.Add(skill);
            }

            for (var i = 0; i < buffSkillCount; i++)
            {
                var skillId = _tableSheets.SkillBuffSheet.Values.First().SkillId;
                var buffSkillRow = _tableSheets.SkillSheet[skillId];
                var buffSkill = (BuffSkill)SkillFactory.Get(buffSkillRow, 0, 0, 0, StatType.NONE);
                item.BuffSkills.Add(buffSkill);
            }

            Assert.Equal(skillCount, item.Skills.Count);

            var materialInfo = new CollectionSheet.RequiredMaterial
            {
                ItemId = row.Id,
                Count = 1,
                Level = 0,
                SkillContains = skillContains,
            };

            Assert.Equal(expected, materialInfo.Validate(item));
        }

        [Fact]
        public void Validate_Costume()
        {
            var row = _tableSheets.ItemSheet.Values.First(r => r.ItemType == ItemType.Costume);
            var item = ItemFactory.CreateItem(row, new TestRandom());
            var materialInfo = new CollectionSheet.RequiredMaterial
            {
                ItemId = row.Id,
                Count = 1,
                Level = 0,
                SkillContains = false,
            };
            Assert.True(materialInfo.Validate((INonFungibleItem)item));
        }

        [Fact]
        public void GetMaterial()
        {
            var collectionMaterials = new List<CollectionSheet.RequiredMaterial>();
            var materials = new List<ICollectionMaterial>();
            for (var i = 0; i < 2; i++)
            {
                var itemId = i + 1;
                var count = 3 - i;
                CollectionSheet.RequiredMaterial requiredMaterial = new ()
                {
                    ItemId = itemId,
                    Count = count,
                    Level = 0,
                    SkillContains = false,
                };
                collectionMaterials.Add(requiredMaterial);
                var material = new FungibleCollectionMaterial
                {
                    ItemId = itemId,
                    ItemCount = count,
                };
                materials.Add(material);
            }

            materials.Reverse();
            for (var index = 0; index < collectionMaterials.Count; index++)
            {
                var collectionMaterial = collectionMaterials[index];
                Assert.Equal(index + 1, collectionMaterial.ItemId);
                Assert.Equal(3 - index, collectionMaterial.Count);
                var m = collectionMaterial.GetMaterial(materials);
                materials.Remove(m);
            }

            Assert.Empty(materials);
        }
    }
}
