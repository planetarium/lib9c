namespace Lib9c.Tests.Action
{
    using Lib9c.Helper;
    using Lib9c.Model.State;
    using Libplanet.Types.Assets;
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
    }
}
