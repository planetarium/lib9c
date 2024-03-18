namespace Lib9c.Tests.Model
{
    using Nekoyume;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.Stat;
    using Nekoyume.TableData;
    using Xunit;

    public class CharacterStatsTest
    {
        private readonly TableSheets _tableSheets;

        public CharacterStatsTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void DeBuffLimit()
        {
            var stats =
                new CharacterStats(
                    _tableSheets.CharacterSheet[GameConfig.DefaultAvatarCharacterId],
                    1);
            var deBuffLimitSheet = new DeBuffLimitSheet();
            deBuffLimitSheet.Set("id,stat_type,percentage\n1,DEF,-50");
            var def = stats.DEF;
            var deBuff = new StatBuff(_tableSheets.StatBuffSheet[503012]);
            stats.AddBuff(deBuff, deBuffLimitSheet: deBuffLimitSheet);
            var limitModifier =
                new StatModifier(StatType.DEF, StatModifier.OperationType.Percentage, -50);
            Assert.Equal(limitModifier.GetModifiedAll(def), stats.DEF);
        }
    }
}
