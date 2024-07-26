#nullable enable

namespace Lib9c.Tests.TableData.CustomEquipmentCraft
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Nekoyume.TableData.CustomEquipmentCraft;
    using Xunit;

    public class CustomEquipmentCraftCostSheetTest
    {
        public static IEnumerable<object?[]> GetTestData()
        {
            yield return new object?[]
            {
                @"relationship,gold_amount,material_1_id,material_1_amount,material_2_id,material_2_amount
10,,600201,1,,",
                0,
                new List<CustomEquipmentCraftCostSheet.MaterialCost>
                {
                    new () { ItemId = 600201, Amount = 1 },
                },
            };
            yield return new object?[]
            {
                @"relationship,gold_amount,material_1_id,material_1_amount,material_2_id,material_2_amount
10,1,,,,",
                1,
                new List<CustomEquipmentCraftCostSheet.MaterialCost>(),
            };
            yield return new object?[]
            {
                @"relationship,gold_amount,material_1_id,material_1_amount,material_2_id,material_2_amount
10,,600201,10,600202,10,",
                0,
                new List<CustomEquipmentCraftCostSheet.MaterialCost>
                {
                    new () { ItemId = 600201, Amount = 10 },
                    new () { ItemId = 600202, Amount = 10 },
                },
            };
            yield return new object?[]
            {
                @"relationship,gold_amount,material_1_id,material_1_amount,material_2_id,material_2_amount
10,10,600201,1,600202,2",
                10,
                new List<CustomEquipmentCraftCostSheet.MaterialCost>
                {
                    new () { ItemId = 600201, Amount = 1 },
                    new () { ItemId = 600202, Amount = 2 },
                },
            };
        }

        [Theory]
        [MemberData(nameof(GetTestData))]
        public void Set(
            string sheetData,
            BigInteger expectedNcgCost,
            List<CustomEquipmentCraftCostSheet.MaterialCost> materialCosts
        )
        {
            var sheet = new CustomEquipmentCraftCostSheet();
            sheet.Set(sheetData);
            Assert.Single(sheet.Values);

            var row = sheet.Values.First();
            Assert.Equal(expectedNcgCost, row.GoldAmount);
            Assert.Equal(materialCosts.Count, row.MaterialCosts.Count);

            foreach (var expected in materialCosts)
            {
                var cost = row.MaterialCosts.First(c => c.ItemId == expected.ItemId);
                Assert.Equal(expected.Amount, cost.Amount);
            }
        }
    }
}
