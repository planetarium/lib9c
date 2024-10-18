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
            // -100% def but limit -50% stats
            var deBuff = new StatBuff(_tableSheets.StatBuffSheet[503012]);
            var groupId = deBuff.RowData.GroupId;
            deBuffLimitSheet.Set($"group_id,percentage\n{groupId},-50");
            var def = stats.DEF;
            stats.AddBuff(deBuff, deBuffLimitSheet);
            var modifier = deBuffLimitSheet[groupId].GetModifier(deBuff.RowData.StatType);
            Assert.Equal(modifier.GetModifiedAll(def), stats.DEF);

            // -500% critical with no limit
            var deBuff2 = new StatBuff(_tableSheets.StatBuffSheet[204003]);
            Assert.True(stats.CRI > 0);
            stats.AddBuff(deBuff2, deBuffLimitSheet);
            Assert.Equal(0, stats.CRI);
        }
    }
}
