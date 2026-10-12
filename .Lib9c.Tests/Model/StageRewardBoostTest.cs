namespace Lib9c.Tests.Model
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class StageRewardBoostTest
    {
        private const string Item = BoostScheduleSheet.Targets.StageItemReward;
        private const string Fav = BoostScheduleSheet.Targets.StageFavReward;

        private readonly TableSheets _tableSheets;

        public StageRewardBoostTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void ApplyItemRewardBoost_ReturnsTheSameListWithoutARow()
        {
            var rewards = Materials((ItemSubType.EquipmentMaterial, 2));

            Assert.Same(
                rewards,
                StageSimulator.ApplyItemRewardBoost(rewards, null, _tableSheets.MaterialItemSheet));
        }

        [Theory]
        [InlineData("MUL", "2", 3, 1, 6, 2)] // exactly twice each drawn item
        [InlineData("MUL", "1.5", 3, 1, 4, 2)] // floor per distinct item, but a single drop becomes 2
        [InlineData("ADD", "1", 3, 1, 4, 2)] // one more of each distinct item
        [InlineData("MUL", "0.5", 3, 1, 1, 0)] // a multiplier below 1 takes items away
        [InlineData("MUL", "100", 3, 1, 30, 10)] // capped at MaxItemRewardBoostFactor
        public void ApplyItemRewardBoost_AdjustsTheCountOfEachDistinctItem(
            string op,
            string value,
            int firstCount,
            int secondCount,
            int expectedFirst,
            int expectedSecond)
        {
            var rows = MaterialRows();
            var rewards = Enumerable.Repeat(rows[0], firstCount)
                .Concat(Enumerable.Repeat(rows[1], secondCount))
                .Select(row => (ItemBase)ItemFactory.CreateMaterial(row))
                .OrderBy(item => item.Id)
                .ToList();
            var boost = StageRewardBoostFixture.Row(Item, op, decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture));

            var boosted = StageSimulator.ApplyItemRewardBoost(
                rewards,
                boost,
                _tableSheets.MaterialItemSheet);

            Assert.Equal(expectedFirst, boosted.Count(item => item.Id == rows[0].Id));
            Assert.Equal(expectedSecond, boosted.Count(item => item.Id == rows[1].Id));
            Assert.Equal(boosted.OrderBy(item => item.Id).Select(item => item.Id), boosted.Select(item => item.Id));
            Assert.Equal(expectedFirst + expectedSecond, boosted.Count);
        }

        [Theory]
        [InlineData("MUL", "2")]
        [InlineData("ADD", "3")]
        [InlineData("MUL", "0.5")]
        public void SplitItemRewardBoost_AddsUpToApplyItemRewardBoost(string op, string value)
        {
            var rows = MaterialRows();
            var rewards = Enumerable.Repeat(rows[0], 3)
                .Concat(Enumerable.Repeat(rows[1], 1))
                .Select(row => (ItemBase)ItemFactory.CreateMaterial(row))
                .ToList();
            var boost = StageRewardBoostFixture.Row(Item, op, decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
            var extraCounts = new Dictionary<int, int>();

            var kept = StageSimulator.SplitItemRewardBoost(rewards, boost, extraCounts);
            var split = kept
                .Concat(StageSimulator.CreateItemRewardBoostExtras(extraCounts, _tableSheets.MaterialItemSheet))
                .OrderBy(item => item.Id)
                .Select(item => item.Id);
            var applied = StageSimulator.ApplyItemRewardBoost(rewards, boost, _tableSheets.MaterialItemSheet)
                .Select(item => item.Id);

            Assert.Equal(applied, split);
            Assert.True(kept.Count <= rewards.Count, "added units are only counted");
        }

        [Fact]
        public void SplitItemRewardBoost_LeavesEverythingAloneWithoutARow()
        {
            var rewards = Materials((ItemSubType.EquipmentMaterial, 2));
            var extraCounts = new Dictionary<int, int>();

            Assert.Same(rewards, StageSimulator.SplitItemRewardBoost(rewards, null, extraCounts));
            Assert.Empty(extraCounts);
        }

        [Theory]
        [InlineData("MUL", "1.2", 1, 2)] // a single drop becomes at least 2 under MUL > 1
        [InlineData("MUL", "1.01", 1, 2)] // however small the excess: effectively 100% here
        [InlineData("MUL", "1.00", 1, 1)]
        [InlineData("MUL", "1.5", 3, 4)]
        [InlineData("MUL", "1.5", 1, 2)]
        [InlineData("MUL", "2", 1, 2)]
        [InlineData("MUL", "3", 1, 3)]
        [InlineData("MUL", "1.2", 2, 2)] // not a general "at least one more": plain floor
        [InlineData("MUL", "1.5", 2, 3)]
        [InlineData("MUL", "1", 1, 1)]
        [InlineData("MUL", "0.5", 1, 0)] // dropped as before
        [InlineData("ADD", "1", 1, 2)] // ADD is unchanged
        [InlineData("ADD", "0", 1, 1)]
        [InlineData("ADD", "-1", 1, 0)]
        [InlineData("MUL", "100", 1, 10)] // still capped at MaxItemRewardBoostFactor
        public void GetBoostedItemRewardCount_TurnsASingleDropIntoTwoUnderMul(
            string op,
            string value,
            int count,
            int expected)
        {
            var boost = StageRewardBoostFixture.Row(Item, op, decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture));

            Assert.Equal(expected, StageSimulator.GetBoostedItemRewardCount(boost, count));
        }

        [Fact]
        public void ApplyItemRewardBoost_BoostsSingleAndDoubleDropsOfOneClearDifferently()
        {
            // Under MUL 1.5 one clear grants 2 of an item drawn once and 3 of an item drawn twice.
            var rows = MaterialRows();
            var rewards = new[] { rows[0], rows[1], rows[1] }
                .Select(row => (ItemBase)ItemFactory.CreateMaterial(row))
                .ToList();

            var boosted = StageSimulator.ApplyItemRewardBoost(
                rewards,
                StageRewardBoostFixture.Row(Item, "MUL", 1.5m),
                _tableSheets.MaterialItemSheet);

            Assert.Equal(2, boosted.Count(item => item.Id == rows[0].Id));
            Assert.Equal(3, boosted.Count(item => item.Id == rows[1].Id));
        }

        [Fact]
        public void ApplyItemRewardBoost_AddsCirclesAsTradableMaterials()
        {
            var circleRow = _tableSheets.MaterialItemSheet.Values
                .First(row => row.ItemSubType == ItemSubType.Circle);
            var rewards = new List<ItemBase> { ItemFactory.CreateTradableMaterial(circleRow) };

            var boosted = StageSimulator.ApplyItemRewardBoost(
                rewards,
                StageRewardBoostFixture.Row(Item, "MUL", 3),
                _tableSheets.MaterialItemSheet);

            Assert.Equal(3, boosted.Count);
            Assert.All(boosted, item => Assert.IsType<TradableMaterial>(item));
        }

        [Fact]
        public void ApplyFavRewardBoost_ReturnsTheSameListWithoutARow()
        {
            var rewards = new List<(string ticker, int amount)> { ("CRYSTAL", 10) };

            Assert.Same(rewards, StageSimulator.ApplyFavRewardBoost(rewards, null));
        }

        [Theory]
        [InlineData("MUL", "2", 10, 20)]
        [InlineData("MUL", "1.5", 3, 4)] // floor
        [InlineData("ADD", "5", 10, 15)]
        [InlineData("MUL", "0.01", 10, null)] // 0 is left out, so nothing mints a zero amount
        public void ApplyFavRewardBoost_AdjustsEachDrawnAmount(
            string op,
            string value,
            int amount,
            int? expected)
        {
            var rewards = new List<(string ticker, int amount)> { ("CRYSTAL", amount), ("RUNE_GOLDENLEAF", 0) };
            var boost = StageRewardBoostFixture.Row(Fav, op, decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture));

            var boosted = StageSimulator.ApplyFavRewardBoost(rewards, boost);

            var crystal = boosted.Where(r => r.ticker == "CRYSTAL").Select(r => (int?)r.amount).SingleOrDefault();
            Assert.Equal(expected, crystal);

            // A ticker drawn as 0 is not a reward the boost may create from nothing.
            Assert.Contains(("RUNE_GOLDENLEAF", 0), boosted);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(5, 3)]
        public void GetWaveRewards_DrawsTheSameAsWithoutARow(int seed, int playCount)
        {
            var stageRow = _tableSheets.StageSheet[1];
            var plainRandom = new CountingRandom(seed);
            var boostedRandom = new CountingRandom(seed);

            var plain = StageSimulator.GetWaveRewards(
                plainRandom,
                stageRow,
                _tableSheets.MaterialItemSheet,
                playCount);
            var boosted = StageSimulator.GetWaveRewards(
                boostedRandom,
                stageRow,
                _tableSheets.MaterialItemSheet,
                playCount,
                StageRewardBoostFixture.Row(Item, "MUL", 2));

            Assert.NotEmpty(plain);
            Assert.Equal(plainRandom.Draws, boostedRandom.Draws);
            Assert.Equal(plainRandom.Next(), boostedRandom.Next());
            Assert.Equal(2 * plain.Count, boosted.Count);
            foreach (var id in plain.Select(item => item.Id).Distinct())
            {
                Assert.Equal(
                    2 * plain.Count(item => item.Id == id),
                    boosted.Count(item => item.Id == id));
            }
        }

        [Fact]
        public void GetWaveRewards_IsUnchangedWithoutARow()
        {
            var stageRow = _tableSheets.StageSheet[1];
            var before = StageSimulator.GetWaveRewards(
                new TestRandom(7),
                stageRow,
                _tableSheets.MaterialItemSheet);
            var after = StageSimulator.GetWaveRewards(
                new TestRandom(7),
                stageRow,
                _tableSheets.MaterialItemSheet,
                itemRewardBoost: null);

            Assert.Equal(before.Select(item => item.Id), after.Select(item => item.Id));
        }

        [Fact]
        public void GetFavWaveRewards_DrawsTheSameAsWithoutARow()
        {
            var stageSheet = new StageSheet();
            stageSheet.Set(StageRewardBoostFixture.WithFavReward(
                TableSheetsImporter.ImportSheets()[nameof(StageSheet)],
                1,
                1000));
            var stageRow = stageSheet[1];
            var plainRandom = new CountingRandom(3);
            var boostedRandom = new CountingRandom(3);

            var plain = StageSimulator.GetFavWaveRewards(plainRandom, stageRow);
            var boosted = StageSimulator.GetFavWaveRewards(
                boostedRandom,
                stageRow,
                StageRewardBoostFixture.Row(Fav, "MUL", 2));

            Assert.Equal(new[] { ("CRYSTAL", 1000) }, plain);
            Assert.Equal(new[] { ("CRYSTAL", 2000) }, boosted);
            Assert.Equal(plainRandom.Draws, boostedRandom.Draws);
            Assert.Equal(plainRandom.Next(), boostedRandom.Next());
        }

        private List<MaterialItemSheet.Row> MaterialRows() =>
            _tableSheets.MaterialItemSheet.Values
                .Where(row => row.ItemSubType == ItemSubType.EquipmentMaterial)
                .OrderBy(row => row.Id)
                .Take(2)
                .ToList();

        private List<ItemBase> Materials((ItemSubType subType, int count) spec) =>
            Enumerable.Repeat(
                    _tableSheets.MaterialItemSheet.Values.First(row => row.ItemSubType == spec.subType),
                    spec.count)
                .Select(row => (ItemBase)ItemFactory.CreateMaterial(row))
                .ToList();

        private class CountingRandom : IRandom
        {
            private readonly TestRandom _random;

            public CountingRandom(int seed)
            {
                _random = new TestRandom(seed);
            }

            public int Draws { get; private set; }

            public int Seed => _random.Seed;

            public int Next()
            {
                Draws++;
                return _random.Next();
            }

            public int Next(int maxValue)
            {
                Draws++;
                return _random.Next(maxValue);
            }

            public int Next(int minValue, int maxValue)
            {
                Draws++;
                return _random.Next(minValue, maxValue);
            }

            public void NextBytes(byte[] buffer)
            {
                Draws++;
                _random.NextBytes(buffer);
            }

            public double NextDouble()
            {
                Draws++;
                return _random.NextDouble();
            }
        }
    }
}
