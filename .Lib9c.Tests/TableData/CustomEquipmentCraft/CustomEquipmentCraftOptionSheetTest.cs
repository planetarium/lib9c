namespace Lib9c.Tests.TableData.CustomEquipmentCraft
{
#nullable enable

    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Model.Item;
    using Lib9c.Model.Stat;
    using Lib9c.TableData.CustomEquipmentCraft;
    using Xunit;

    public class CustomEquipmentCraftOptionSheetTest
    {
        public static IEnumerable<object?[]> GetOptionTestData()
        {
            yield return new object?[]
            {
                @"id,item_sub_type,ratio,hp_ratio,atk_ratio,def_ratio,cri_ratio,hit_ratio,spd_ratio,drv_ratio,drr_ratio,cdmg_ratio,ap_ratio,thorn_ratio
1,Weapon,50,,100,,,,,,,,,",
                ItemSubType.Weapon,
                50,
                100,
                new List<CustomEquipmentCraftOptionSheet.SubStat>
                {
                    new () { StatType = StatType.ATK, Ratio = 100, },
                },
            };

            yield return new object?[]
            {
                @"id,item_sub_type,ratio,hp_ratio,atk_ratio,def_ratio,cri_ratio,hit_ratio,spd_ratio,drv_ratio,drr_ratio,cdmg_ratio,ap_ratio,thorn_ratio
1,Weapon,100,,50,,,50,,,,,,",
                ItemSubType.Weapon,
                100,
                100,
                new List<CustomEquipmentCraftOptionSheet.SubStat>
                {
                    new () { StatType = StatType.ATK, Ratio = 50, },
                    new () { StatType = StatType.HIT, Ratio = 50, },
                },
            };

            yield return new object?[]
            {
                @"id,item_sub_type,ratio,hp_ratio,atk_ratio,def_ratio,cri_ratio,hit_ratio,spd_ratio,drv_ratio,drr_ratio,cdmg_ratio,ap_ratio,thorn_ratio
1,Armor,50,,,100,,,,,,,,",
                ItemSubType.Armor,
                50,
                100,
                new List<CustomEquipmentCraftOptionSheet.SubStat>
                {
                    new () { StatType = StatType.DEF, Ratio = 100, },
                },
            };
        }

        [Theory]
        [MemberData(nameof(GetOptionTestData))]
        public void Set(
            string sheetData,
            ItemSubType expectedItemSubType,
            int expectedRatio,
            int expectedTotalRatio,
            List<CustomEquipmentCraftOptionSheet.SubStat> subStats
        )
        {
            var sheet = new CustomEquipmentCraftOptionSheet();
            sheet.Set(sheetData);
            Assert.Single(sheet.Values);

            var row = sheet.Values.First();

            Assert.Equal(1, row.Id);
            Assert.Equal(expectedItemSubType, row.ItemSubType);
            Assert.Equal(expectedRatio, row.Ratio);
            Assert.Equal(expectedTotalRatio, row.TotalOptionRatio);
            Assert.Equal(subStats.Count, row.SubStatData.Count);
            foreach (var expected in subStats)
            {
                var substat = row.SubStatData.First(d => d.StatType == expected.StatType);
                Assert.Equal(expected.Ratio, substat.Ratio);
            }
        }
    }
}
