namespace Lib9c.Tests.Action
{
    using System;
    using Libplanet.Types.Assets;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class RuneHelperTest
    {
        private readonly TableSheets _tableSheets = new (TableSheetsImporter.ImportSheets());

        [Theory]
        [InlineData(50, 0)]
        [InlineData(500, 0)]
        [InlineData(5000, 0)]
        [InlineData(50000, 8)]
        [InlineData(500000, 83)]
        public void CalculateStakeReward(int amountGold, int expected)
        {
            var ncgCurrency = Currency.Legacy("NCG", 2, null);
            Assert.Equal(expected * RuneHelper.StakeRune, RuneHelper.CalculateStakeReward(amountGold * ncgCurrency, 6000));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 30000)]
        [InlineData(100, 34554)] // 1*30000 + 99*46
        [InlineData(130200, 1043056)] // Max level
        public void CalculateRuneLevelBonus(int runeLevel, int expectedBonus)
        {
            var runeStates = new AllRuneState(30001, runeLevel);
            var runeLevelBonus = RuneHelper.CalculateRuneLevelBonus(
                runeStates,
                _tableSheets.RuneListSheet,
                _tableSheets.RuneLevelBonusSheet
            );
            Assert.Equal(expectedBonus, runeLevelBonus);
        }

        [Theory]
        [InlineData("MUL", "0.7", 4, 2)] // 2.8 rounds down
        [InlineData("MUL", "0.6", 20000, 12000)]
        [InlineData("MUL", "0.2", 4, 1)] // 0.8 would make the try free; kept at 1
        [InlineData("MUL", "0.0001", 1, 1)]
        [InlineData("ADD", "-10", 4, 1)]
        [InlineData("ADD", "-1", 4, 3)]
        [InlineData("MUL", "1.5", 100, 100)] // discount only
        [InlineData("ADD", "2147483647", 100, 100)]
        [InlineData("MUL", "0.5", 0, 0)] // a free cost stays free
        [InlineData("ADD", "5", 0, 0)] // and a boost never creates one
        public void ApplyEnhancementCostBoostQuantity(
            string op,
            string value,
            int baseCost,
            int expected)
        {
            var boost = CreateBoostSheet($"1,RUNE_ENHANCEMENT_CRYSTAL_COST,*,{op},{value},0,10")[1];
            Assert.Equal(expected, RuneHelper.ApplyEnhancementCostBoostQuantity(boost, baseCost));
        }

        [Fact]
        public void ApplyEnhancementCostBoostQuantity_WithoutBoost_IsUnchanged()
        {
            Assert.Equal(4, RuneHelper.ApplyEnhancementCostBoostQuantity(null, 4));
        }

        [Theory]
        // level, expected crystal, expected rune stone; base cost is 20000 crystal, 240 stones.
        [InlineData(1, 12000, 168)]
        [InlineData(50, 12000, 168)]
        [InlineData(51, 13000, 192)]
        [InlineData(100, 13000, 192)]
        [InlineData(101, 15000, 216)]
        [InlineData(201, 16000, 240)]
        [InlineData(300, 16000, 240)]
        [InlineData(301, 20000, 240)] // no row covers it
        public void ApplyEnhancementCostBoost_FollowsTiersByTargetLevel(
            int level,
            int expectedCrystal,
            int expectedRuneStone)
        {
            var cost = new RuneCostSheet.RuneCostData(level, level, 240, 20000, 50, 7000);

            var boosted = RuneHelper.ApplyEnhancementCostBoost(cost, level, TieredSheet(), 150);

            Assert.Equal(expectedCrystal, boosted.CrystalQuantity);
            Assert.Equal(expectedRuneStone, boosted.RuneStoneQuantity);
            Assert.Equal(50, boosted.NcgQuantity);
            Assert.Equal(7000, boosted.LevelUpSuccessRate);
            Assert.Equal(level, boosted.LevelStart);
            Assert.Equal(level, boosted.LevelEnd);
        }

        [Theory]
        [InlineData(99)] // before the schedule
        [InlineData(200)] // end_block is exclusive
        public void ApplyEnhancementCostBoost_OutsideTheSchedule_ReturnsTheSameCost(long blockIndex)
        {
            var cost = new RuneCostSheet.RuneCostData(1, 1, 240, 20000, 50, 7000);

            Assert.Same(cost, RuneHelper.ApplyEnhancementCostBoost(cost, 1, TieredSheet(), blockIndex));
            Assert.Same(cost, RuneHelper.ApplyEnhancementCostBoost(cost, 1, null, 150));
        }

        [Fact]
        public void ApplyEnhancementCostBoost_IgnoresOtherTargets()
        {
            var sheet = CreateBoostSheet(
                "1,RUNE_SUMMON_GUARANTEE,*,MUL,0.5,0,10",
                "2,RUNE_ENHANCEMENT_NCG_COST,*,MUL,0.5,0,10");
            var cost = new RuneCostSheet.RuneCostData(1, 1, 240, 20000, 50, 7000);

            Assert.Same(cost, RuneHelper.ApplyEnhancementCostBoost(cost, 1, sheet, 5));
        }

        [Fact]
        public void ApplyEnhancementCostBoost_AdjustsOnlyTheScheduledCurrency()
        {
            var sheet = CreateBoostSheet("1,RUNE_ENHANCEMENT_RUNE_STONE_COST,1~10,MUL,0.5,0,10");
            var cost = new RuneCostSheet.RuneCostData(1, 1, 240, 20000, 50, 7000);

            var boosted = RuneHelper.ApplyEnhancementCostBoost(cost, 1, sheet, 5);

            Assert.Equal(120, boosted.RuneStoneQuantity);
            Assert.Equal(20000, boosted.CrystalQuantity);
        }

        [Fact]
        public void TryEnhancement_AppliesBoostPerTriedLevelAndKeepsTheRandomDraws()
        {
            // Levels 49..52, each try succeeding 50% of the time, so a failed try repeats its
            // level and must be charged that level's (boosted) cost again.
            var costSheet = new RuneCostSheet();
            costSheet.Set(
                "id,level_start,level_end,rune_stone_quantity,crystal_quantity,ncg_quantity,level_up_success_rate\n" +
                "1,1,48,1,1,1,10000\n" +
                "1,49,50,240,20000,0,5000\n" +
                "1,51,52,180,15000,50,5000\n");
            var costRow = costSheet[1];
            const int seed = 7;
            const int tryCount = 6;

            var baseRandom = new TestRandom(seed);
            Assert.True(RuneHelper.TryEnhancement(48, costRow, baseRandom, tryCount, out var baseResult));
            var boostRandom = new TestRandom(seed);
            Assert.True(RuneHelper.TryEnhancement(
                48,
                costRow,
                boostRandom,
                tryCount,
                out var boostResult,
                TieredSheet(),
                150));

            // Same draws, same outcome, same NCG.
            Assert.Equal(baseRandom.Next(), boostRandom.Next());
            Assert.Equal(baseResult.LevelUpCount, boostResult.LevelUpCount);
            Assert.Equal(baseResult.NcgCost, boostResult.NcgCost);

            // Recompute the expected costs try by try from the same draws.
            var replay = new TestRandom(seed);
            var level = 48;
            var expectedCrystal = 0;
            var expectedRuneStone = 0;
            var failures = 0;
            var highestTarget = 0;
            for (var i = 0; i < tryCount; i++)
            {
                var target = level + 1;
                highestTarget = Math.Max(highestTarget, target);
                var discounted = target <= 50;
                expectedCrystal += discounted ? 12000 : 9750;
                expectedRuneStone += discounted ? 168 : 144;
                if (replay.Next(0, 10000) < 5000)
                {
                    level++;
                }
                else
                {
                    failures++;
                }
            }

            // The seed must exercise both a recharged level and the 51+ tier, or the expected
            // costs above prove nothing about them.
            Assert.True(failures > 0, "the seed never fails a try");
            Assert.True(highestTarget > 50, $"the seed never reaches the 51+ tier ({highestTarget})");

            Assert.Equal(expectedCrystal, boostResult.CrystalCost);
            Assert.Equal(expectedRuneStone, boostResult.RuneCost);
            Assert.True(boostResult.CrystalCost < baseResult.CrystalCost);
        }

        [Fact]
        public void TryEnhancement_WithoutBoost_IsUnchanged()
        {
            var costRow = _tableSheets.RuneCostSheet[10001];
            var expectedRandom = new TestRandom(3);
            RuneHelper.TryEnhancement(48, costRow, expectedRandom, 4, out var expected);

            var emptyRandom = new TestRandom(3);
            RuneHelper.TryEnhancement(48, costRow, emptyRandom, 4, out var withEmptySheet, new BoostScheduleSheet(), 150);
            var outsideRandom = new TestRandom(3);
            RuneHelper.TryEnhancement(48, costRow, outsideRandom, 4, out var outsideSchedule, TieredSheet(), 250);

            Assert.Equal(expected.ToString(), withEmptySheet.ToString());
            Assert.Equal(expected.ToString(), outsideSchedule.ToString());
            Assert.Equal(expectedRandom.Next(), emptyRandom.Next());
        }

        /// <summary>
        /// The rotation event E3 curve, scheduled for blocks [100, 200).
        /// </summary>
        private static BoostScheduleSheet TieredSheet() =>
            CreateBoostSheet(
                "1,RUNE_ENHANCEMENT_CRYSTAL_COST,1~50,MUL,0.60,100,200",
                "2,RUNE_ENHANCEMENT_CRYSTAL_COST,51~100,MUL,0.65,100,200",
                "3,RUNE_ENHANCEMENT_CRYSTAL_COST,101~200,MUL,0.75,100,200",
                "4,RUNE_ENHANCEMENT_CRYSTAL_COST,201~300,MUL,0.80,100,200",
                "5,RUNE_ENHANCEMENT_RUNE_STONE_COST,1~50,MUL,0.70,100,200",
                "6,RUNE_ENHANCEMENT_RUNE_STONE_COST,51~100,MUL,0.80,100,200",
                "7,RUNE_ENHANCEMENT_RUNE_STONE_COST,101~200,MUL,0.90,100,200",
                "8,RUNE_ENHANCEMENT_RUNE_STONE_COST,201~300,MUL,1.00,100,200");

        private static BoostScheduleSheet CreateBoostSheet(params string[] rows)
        {
            var sheet = new BoostScheduleSheet();
            sheet.Set(
                "id,target,target_id,op,value,start_block,end_block\n" +
                string.Join("\n", rows));
            return sheet;
        }
    }
}
