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

        [Fact]
        public void BuffLimit()
        {
            var stats =
                new CharacterStats(
                    _tableSheets.CharacterSheet[GameConfig.DefaultAvatarCharacterId],
                    1);
            var deBuffLimitSheet = new DeBuffLimitSheet();
            // +60% atk with no limit
            var buff = new StatBuff(_tableSheets.StatBuffSheet[503011]);
            var groupId = buff.RowData.GroupId;
            var atk = stats.ATK;
            stats.AddBuff(buff, deBuffLimitSheet);
            Assert.Equal(atk * 1.6m, stats.ATK);

            // reset stats
            stats.RemoveBuff(buff);
            Assert.Equal(atk, stats.ATK);
            // +60% atk but limit 50% stats
            deBuffLimitSheet.Set($"group_id,percentage\n{groupId},50");
            var modifier = deBuffLimitSheet[groupId].GetModifier(buff.RowData.StatType);
            stats.AddBuff(buff, deBuffLimitSheet);
            Assert.Equal(atk * 1.5m, modifier.GetModifiedAll(atk));
            Assert.Equal(atk * 1.5m, stats.ATK);
        }
    }
}
