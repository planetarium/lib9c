namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using Libplanet.Types.Assets;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class RuneHelperTest
    {
        private readonly Currency _crystalCurrency = CrystalCalculator.CRYSTAL;

        private readonly TableSheets _tableSheets =
            new TableSheets(TableSheetsImporter.ImportSheets());

        [Theory]
        [InlineData(typeof(WorldBossRankRewardSheet))]
        [InlineData(typeof(WorldBossKillRewardSheet))]
        public void CalculateReward(Type sheetType)
        {
            var random = new TestRandom();
            IWorldBossRewardSheet sheet;
            if (sheetType == typeof(WorldBossRankRewardSheet))
            {
                sheet = _tableSheets.WorldBossRankRewardSheet;
            }
            else
            {
                sheet = _tableSheets.WorldBossKillRewardSheet;
            }

            foreach (var rewardRow in sheet.OrderedRows)
            {
                var bossId = rewardRow.BossId;
                var rank = rewardRow.Rank;
                var fungibleAssetValues = RuneHelper.CalculateReward(
                    rank,
                    bossId,
                    _tableSheets.RuneWeightSheet,
                    sheet,
                    _tableSheets.RuneSheet,
                    random
                );
                var expectedRune = rewardRow.Rune;
                var expectedCrystal = rewardRow.Crystal * _crystalCurrency;
                var crystal = fungibleAssetValues.First(f => f.Currency.Equals(_crystalCurrency));
                var rune = fungibleAssetValues
                    .Where(f => !f.Currency.Equals(_crystalCurrency))
                    .Sum(r => (int)r.MajorUnit);

                Assert.Equal(expectedCrystal, crystal);
                Assert.Equal(expectedRune, rune);
            }
        }

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
    }
}
