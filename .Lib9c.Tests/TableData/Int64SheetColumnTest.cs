namespace Lib9c.Tests.TableData
{
    using System;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Crystal;
    using Nekoyume.TableData.CustomEquipmentCraft;
    using Xunit;

    /// <summary>
    /// Columns that grow with every equipment grade are parsed as <see cref="long"/>, so a value
    /// past <see cref="int.MaxValue"/> loads instead of failing the whole sheet.
    /// </summary>
    public class Int64SheetColumnTest
    {
        private const long Big = 3_000_000_000L;

        // TestRandom(42).NextLong(Big, Big * 2): the 8 bytes from System.Random(42), read
        // big-endian, modulo Big.
        private const long PinnedWideDraw = 3_143_288_123L;

        [Fact]
        public void CrystalSheets()
        {
            var materialCost = new CrystalMaterialCostSheet();
            materialCost.Set($"item_id,crystal\n303000,{Big}\n");
            Assert.Equal(Big, materialCost[303000].CRYSTAL);

            var grinding = new CrystalEquipmentGrindingSheet();
            grinding.Set(
                "id,enchant_base_id,gain_crystal,reward_material1,count1,reward_material2,count2\n" +
                $"10100000,10100000,{Big},,,,\n");
            Assert.Equal(Big, grinding[10100000].CRYSTAL);

            var hammer = new CrystalHammerPointSheet();
            hammer.Set($"recipe_ID,max_hammer_count,crystal_cost\n1,4,{Big}\n");
            Assert.Equal(Big, hammer[1].CRYSTAL);

            var recipe = new EquipmentItemRecipeSheet();
            recipe.Set(
                "id,result_equipment_id,material_id,material_count,required_action_point,required_gold,required_block_index,unlock_stage,sub_recipe_id,sub_recipe_id_2,sub_recipe_id_3,required_crystal,item_sub_type\n" +
                $"1,10110000,303000,2,0,0,5,3,373,374,,{Big},Weapon\n");
            Assert.Equal(Big, recipe[1].CRYSTAL);
        }

        [Fact]
        public void EquipmentItemOptionSheet()
        {
            var sheet = new EquipmentItemOptionSheet();
            sheet.Set(
                "id,stat_type,stat_min,stat_max,skill_id,skill_damage_min,skill_damage_max,skill_chance_min,skill_chance_max,stat_damage_ratio_min,stat_damage_ratio_max,referenced_stattype\n" +
                $"1,ATK,{Big},{Big + 1},,,,,,,,\n" +
                $"2,,,,100001,{Big},{Big + 1},10,20,,,\n");
            Assert.Equal(Big, sheet[1].StatMin);
            Assert.Equal(Big + 1, sheet[1].StatMax);
            Assert.Equal(Big, sheet[2].SkillDamageMin);
            Assert.Equal(Big + 1, sheet[2].SkillDamageMax);
        }

        [Fact]
        public void CustomEquipmentCraftRelationshipSheet()
        {
            var sheet = new CustomEquipmentCraftRelationshipSheet();
            sheet.Set(
                "relationship,cost_multiplier,required_block_multiplier,min_cp_1,max_cp_1,ratio_1,min_cp_2,max_cp_2,ratio_2,min_cp_3,max_cp_3,ratio_3,min_cp_4,max_cp_4,ratio_4,min_cp_5,max_cp_5,ratio_5,min_cp_6,max_cp_6,ratio_6,min_cp_7,max_cp_7,ratio_7,min_cp_8,max_cp_8,ratio_8,min_cp_9,max_cp_9,ratio_9,min_cp_10,max_cp_10,ratio_10,weapon_item_id,armor_item_id,belt_item_id,necklace_item_id,ring_item_id,gold_amount,material_1_id,material_1_amount,material_2_id,material_2_amount\n" +
                $"0,10000,10000,{Big},{Big + 100},20,,,,,,,,,,,,,,,,,,,,,,,,,,,,20160000,20260000,20360000,20460000,20560000,0,,,,\n");
            var group = sheet[0].CpGroups.Single();
            Assert.Equal(Big, group.MinCp);
            Assert.Equal(Big + 100, group.MaxCp);

            var cp = CustomCraftHelper.SelectCp(sheet[0], new TestRandom());
            Assert.InRange(cp, Big, Big + 100);
        }

        [Fact]
        public void RuneSheets()
        {
            var cost = new RuneCostSheet();
            cost.Set(
                "id,level_start,level_end,rune_stone_quantity,crystal_quantity,ncg_quantity,level_up_success_rate\n" +
                $"10001,1,1,400,{Big},0,10000\n");
            Assert.True(cost[10001].TryGetCost(1, out var costData));
            Assert.Equal(Big, costData.CrystalQuantity);

            var option = new RuneOptionSheet();
            option.Set(
                "rune_id,level,total_cp,stat_type_1,value_1,value_type_1,stat_type_2,value_2,value_type_2,stat_type_3,value_3,value_type3,skillId,cooldown,chance,skill_value,skill_stat_ratio,skill_value_type,stat_reference_type,buff_duration\n" +
                $"10051,1,{Big},ATK,1184221,Add,,,,,,,,,,,,,,\n");
            Assert.Equal(Big, option[10051].LevelOptionMap[1].Cp);
        }

        [Fact]
        public void RuneEnhancement_AccumulatesCrystalBeyondInt32()
        {
            var cost = new RuneCostSheet();
            cost.Set(
                "id,level_start,level_end,rune_stone_quantity,crystal_quantity,ncg_quantity,level_up_success_rate\n" +
                "10001,1,3,1,1000000000,0,10000\n");

            Assert.True(RuneHelper.TryEnhancement(0, cost[10001], new TestRandom(), 3, out var result));
            Assert.Equal(3, result.LevelUpCount);
            Assert.Equal(3_000_000_000L, result.CrystalCost);
        }

        [Fact]
        public void CollectionSheet()
        {
            var sheet = new CollectionSheet();
            sheet.Set(
                "id,item_id1,count1,level1,skill1,item_id2,count2,level2,skill2,item_id3,count3,level3,skill3,item_id4,count4,level4,skill4,item_id5,count5,level5,skill5,item_id6,count6,level6,skill6,stat_type1,modify_type1,modify_value1,stat_type2,modify_type2,modify_value2,stat_type3,modify_type3,modify_value3\n" +
                $"1,10110000,1,0,TRUE,,,,,,,,,,,,,,,,,,,,,HP,Add,{Big},,,,,,\n");
            Assert.Equal(Big, sheet[1].StatModifiers.Single().Value);
        }

        [Fact]
        public void CalculateMaterialCost_BeyondInt32()
        {
            // 10,000,000 × 300 = 3,000,000,000 used to wrap negative in int before reaching
            // the currency, which skipped the charge.
            var sheet = new CrystalMaterialCostSheet();
            sheet.Set("item_id,crystal\n393002,10000000\n");

            var cost = CrystalCalculator.CalculateMaterialCost(393002, 300, sheet);

            Assert.Equal(CrystalCalculator.CRYSTAL * Big, cost);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(10, 100)]
        [InlineData(-5, 5)]
        [InlineData(int.MinValue, int.MaxValue)]
        public void NextLong_WithinInt32_MatchesNext(int minValue, int maxValue)
        {
            for (var seed = 0; seed < 20; seed++)
            {
                var expected = new TestRandom(seed).Next(minValue, maxValue);
                var actual = new TestRandom(seed).NextLong(minValue, maxValue);
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void NextLong_Int32Boundary()
        {
            // (0, int.MaxValue) still fits and must keep using Next(int, int).
            Assert.Equal(
                new TestRandom(7).Next(0, int.MaxValue),
                new TestRandom(7).NextLong(0, int.MaxValue));

            // StatMax = int.MaxValue makes the exclusive bound int.MaxValue + 1, which the old
            // int arithmetic wrapped to int.MinValue; it now takes the wide path.
            var value = new TestRandom(7).NextLong(int.MaxValue, (long)int.MaxValue + 1);
            Assert.Equal(int.MaxValue, value);
        }

        [Fact]
        public void NextLong_BeyondInt32_IsPinned()
        {
            // Every node must draw the same value; this pins the byte order and the modulo.
            Assert.Equal(PinnedWideDraw, new TestRandom(42).NextLong(Big, Big * 2));
        }

        [Fact]
        public void NextLong_BeyondInt32_StaysInRange()
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var value = new TestRandom(seed).NextLong(Big, Big + 10);
                Assert.InRange(value, Big, Big + 9);
            }
        }

        [Fact]
        public void NextLong_EmptyRange_ReturnsMin()
        {
            Assert.Equal(Big, new TestRandom().NextLong(Big, Big));
        }

        [Fact]
        public void NextLong_MinGreaterThanMax_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TestRandom().NextLong(Big + 1, Big));
        }
    }
}
