namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using Libplanet.Assets;
    using Nekoyume.Helper;
    using Nekoyume.TableData;
    using Xunit;

    public class RuneHelperTest
    {
        private readonly Currency _crystalCurrency = CrystalCalculator.CRYSTAL;

        [Theory]
        [InlineData(typeof(WorldBossRankRewardSheet))]
        [InlineData(typeof(WorldBossKillRewardSheet))]
        public void CalculateReward(Type sheetType)
        {
            var tableSheet = new TableSheets(TableSheetsImporter.ImportSheets());
            var random = new TestRandom();
            IWorldBossRewardSheet sheet;
            if (sheetType == typeof(WorldBossRankRewardSheet))
            {
                sheet = tableSheet.WorldBossRankRewardSheet;
            }
            else
            {
                sheet = tableSheet.WorldBossKillRewardSheet;
            }

            foreach (var rewardRow in sheet.OrderedRows)
            {
                var bossId = rewardRow.BossId;
                var rank = rewardRow.Rank;
                var fungibleAssetValues = RuneHelper.CalculateReward(
                    rank,
                    bossId,
                    tableSheet.RuneWeightSheet,
                    sheet,
                    tableSheet.RuneSheet,
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
    }
}
