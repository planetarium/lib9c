namespace Lib9c.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Helper;
    using Lib9c.Model.Item;
    using Lib9c.Model.State;
    using Lib9c.TableData.Crystal;
    using Lib9c.TableData.Item;
    using Lib9c.TableData.WorldAndStage;
    using Libplanet.Types.Assets;
    using Xunit;

    public class CrystalCalculatorTest
    {
        private readonly TableSheets _tableSheets;
        private readonly EquipmentItemRecipeSheet _equipmentItemRecipeSheet;
        private readonly WorldUnlockSheet _worldUnlockSheet;
        private readonly CrystalMaterialCostSheet _crystalMaterialCostSheet;
        private readonly Currency _ncgCurrency;

        public CrystalCalculatorTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _equipmentItemRecipeSheet = _tableSheets.EquipmentItemRecipeSheet;
            _worldUnlockSheet = _tableSheets.WorldUnlockSheet;
            _crystalMaterialCostSheet = _tableSheets.CrystalMaterialCostSheet;
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _ncgCurrency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void CalculateRecipeUnlockCost(int recipeCount)
        {
            var recipeIds = new List<int>();
            var expected = 0;
            foreach (var row in _equipmentItemRecipeSheet.OrderedList)
            {
                if (recipeIds.Count < recipeCount)
                {
                    recipeIds.Add(row.Id);
                    expected += row.CRYSTAL;
                }
                else
                {
                    break;
                }
            }

            Assert.Equal(expected * CrystalCalculator.CRYSTAL, CrystalCalculator.CalculateRecipeUnlockCost(recipeIds, _equipmentItemRecipeSheet));
        }

        [Theory]
        [InlineData(new[] { 2, }, 500)]
        [InlineData(new[] { 2, 3, }, 3000)]
        public void CalculateWorldUnlockCost(IEnumerable<int> worldIds, int expected)
        {
            Assert.Equal(expected * CrystalCalculator.CRYSTAL, CrystalCalculator.CalculateWorldUnlockCost(worldIds, _worldUnlockSheet));
        }

        [Theory]
        [ClassData(typeof(CalculateCrystalData))]
        public void CalculateCrystal((int EquipmentId, int Level)[] equipmentInfos, int stakedAmount, bool enhancementFailed, string expected)
        {
            var equipmentList = new List<Equipment>();
            foreach (var (equipmentId, level) in equipmentInfos)
            {
                var row = _tableSheets.EquipmentItemSheet[equipmentId];
                var equipment =
                    ItemFactory.CreateItemUsable(row, default, 0, level);
                equipmentList.Add((Equipment)equipment);
            }

            var actual = CrystalCalculator.CalculateCrystal(
                default,
                equipmentList,
                stakedAmount * _ncgCurrency,
                enhancementFailed,
                _tableSheets.CrystalEquipmentGrindingSheet,
                _tableSheets.CrystalMonsterCollectionMultiplierSheet,
                _tableSheets.StakeRegularRewardSheet
            );

            Assert.Equal(
                FungibleAssetValue.Parse(Currencies.Crystal, expected),
                actual);
        }

        [Theory]
        [InlineData(200, 100, 200, true, true)]
        [InlineData(900, 1000, 90, true, true)]
        // Minimum
        [InlineData(100, 200, 50, true, true)]
        // Maximum
        [InlineData(300, 100, 300, true, true)]
        // Avoid DivideByZeroException
        [InlineData(0, 100, 100, true, true)]
        [InlineData(100, 0, 100, true, true)]
        [InlineData(0, 0, 100, true, true)]
        [InlineData(100, 0, 100, true, false)]
        [InlineData(0, 0, 100, false, true)]
        [InlineData(0, 0, 100, false, false)]
        public void CalculateCombinationCost(int psCrystal, int bpsCrystal, int expected, bool psExist, bool bpsExist)
        {
            var crystal = 100 * CrystalCalculator.CRYSTAL;
            var ps = psExist
                ? new CrystalCostState(default, psCrystal * CrystalCalculator.CRYSTAL)
                : null;
            var bps = bpsExist
                ? new CrystalCostState(default, bpsCrystal * CrystalCalculator.CRYSTAL)
                : null;
            var row = _tableSheets.CrystalFluctuationSheet.Values.First(
                r =>
                    r.Type == CrystalFluctuationSheet.ServiceType.Combination);
            Assert.Equal(
                expected * CrystalCalculator.CRYSTAL,
                CrystalCalculator.CalculateCombinationCost(crystal, row, ps, bps)
            );
        }

        [Fact]
        public void CalculateRandomBuffCost()
        {
            var stageBuffGachaSheet = _tableSheets.CrystalStageBuffGachaSheet;
            foreach (var row in stageBuffGachaSheet.Values)
            {
                Assert.Equal(
                    row.NormalCost * CrystalCalculator.CRYSTAL,
                    CrystalCalculator.CalculateBuffGachaCost(row.StageId, false, stageBuffGachaSheet));
                Assert.Equal(
                    row.AdvancedCost * CrystalCalculator.CRYSTAL,
                    CrystalCalculator.CalculateBuffGachaCost(row.StageId, true, stageBuffGachaSheet));
            }
        }

        [Theory]
        [InlineData(303000, 1, 75, null)]
        [InlineData(303003, 2, 90000, null)]
        [InlineData(306068, 1, 100, typeof(ArgumentException))]
        public void CalculateMaterialCost(int materialId, int materialCount, int expected, Type exc)
        {
            if (_crystalMaterialCostSheet.ContainsKey(materialId))
            {
                var cost = CrystalCalculator.CalculateMaterialCost(materialId, materialCount, _crystalMaterialCostSheet);
                Assert.Equal(expected * CrystalCalculator.CRYSTAL, cost);
            }
            else
            {
                Assert.Throws(exc, () => CrystalCalculator.CalculateMaterialCost(materialId, materialCount, _crystalMaterialCostSheet));
            }
        }

        private class CalculateCrystalData : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new ()
            {
                // 1 + ((2^0 - 1) * 1) = 1
                // enchant level 2
                // 10 + ((2^2 - 1) * 10) = 40
                // total 41
                new object[]
                {
                    new[]
                    {
                        (10100000, 0),
                        (10110000, 2),
                    },
                    10,
                    false,
                    "41",
                },
                new object[]
                {
                    // enchant failed
                    // (1 + (2^0 -1) * 1) / 2 = 0.5
                    // total 0.5
                    new[]
                    {
                        (10100000, 0),
                    },
                    10,
                    true,
                    "0.5",
                },
                // enchant level 3 & failed
                // (10 + (2^3 - 1) * 10) / 2 = 40
                // multiply by staking level 2
                // 40 * 0.5 = 20
                // total 60
                new object[]
                {
                    new[]
                    {
                        (10110000, 3),
                    },
                    500,
                    true,
                    "60",
                },
                // enchant level 1
                // 1 + (2^1 - 1) * 1 = 2
                // enchant level 2
                // 10 + (2^2 - 1) * 10 = 40
                // multiply by staking level 2
                // 42 * 0.5 = 21
                // total 63
                new object[]
                {
                    new[]
                    {
                        (10100000, 1),
                        (10110000, 2),
                    },
                    500,
                    false,
                    "63",
                },
                // enchant level 1
                // 10 + (2^1 - 1) * 10 = 20
                new object[]
                {
                    new[]
                    {
                        (10110000, 1),
                    },
                    0,
                    false,
                    "20",
                },
                // Max level exponent = 5
                // 10 + (2^20 - 1) * 10 changes to
                // 10 + (2^5 - 1) * 10 = 320
                new object[]
                {
                    new[]
                    {
                        (10110000, 20),
                    },
                    0,
                    false,
                    "320",
                },
                // Max crystal = 100_000_000
                // 10_000_000 + (2^5 - 1) * 10_000_000 = 320_000_000
                // limit to 100_000_000
                new object[]
                {
                    new[]
                    {
                        (10650006, 5),
                    },
                    0,
                    false,
                    "100000000",
                },
            };

            public IEnumerator<object[]> GetEnumerator()
            {
                return _data.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _data.GetEnumerator();
            }
        }
    }
}
