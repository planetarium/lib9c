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
        public void DeBuffLimitPercentage()
        {
            var stats =
                new CharacterStats(
                    _tableSheets.CharacterSheet[GameConfig.DefaultAvatarCharacterId],
                    1);
            var buffLimitSheet = new BuffLimitSheet();
            // -100% def but limit -50% stats
            var deBuff = new StatBuff(_tableSheets.StatBuffSheet[503012]);
            var groupId = deBuff.RowData.GroupId;
            buffLimitSheet.Set($"group_id,percentage\n{groupId},Percentage,-50");
            var def = stats.DEF;
            stats.AddBuff(deBuff, buffLimitSheet);
            var modifier = buffLimitSheet[groupId].GetModifier(deBuff.RowData.StatType);
            Assert.Equal(modifier.GetModifiedAll(def), stats.DEF);

            // -500% critical with no limit
            var deBuff2 = new StatBuff(_tableSheets.StatBuffSheet[204003]);
            Assert.True(stats.CRI > 0);
            stats.AddBuff(deBuff2, buffLimitSheet);
            Assert.Equal(0, stats.CRI);
        }

        [Fact]
        public void BuffLimitPercentage()
        {
            var stats =
                new CharacterStats(
                    _tableSheets.CharacterSheet[GameConfig.DefaultAvatarCharacterId],
                    1);
            var buffLimitSheet = new BuffLimitSheet();
            // +60% atk with no limit
            var buff = new StatBuff(_tableSheets.StatBuffSheet[503011]);
            var groupId = buff.RowData.GroupId;
            var atk = stats.ATK;
            stats.AddBuff(buff, buffLimitSheet);
            Assert.Equal(atk * 1.6m, stats.ATK);

            // reset stats
            stats.RemoveBuff(buff);
            Assert.Equal(atk, stats.ATK);
            // +60% atk but limit 50% stats
            buffLimitSheet.Set($"group_id,modify_type,percentage\n{groupId},Percentage,50");
            var modifier = buffLimitSheet[groupId].GetModifier(buff.RowData.StatType);
            stats.AddBuff(buff, buffLimitSheet);
            Assert.Equal(atk * 1.5m, modifier.GetModifiedAll(atk));
            Assert.Equal(atk * 1.5m, stats.ATK);
        }

        [Fact]
        public void BuffLimitAdd()
        {
            const long buffDrrValue = 80;
            const long buffLimitValue = 50;

            var stats =
                new CharacterStats(
                    _tableSheets.CharacterSheet[GameConfig.DefaultAvatarCharacterId],
                    1);
            var statBuffSheet = new StatBuffSheet();
            statBuffSheet.Set($"id,group,_name,chance,duration,target_type,stat_type,modify_type,modify_value,is_enhanceable,max_stack\n702001,702001,DRR (커스텀 장비),100,12,Self,DRR,Add,{buffDrrValue},true,");
            var buffLimitSheet = new BuffLimitSheet();
            // +60% atk with no limit
            var buff = new StatBuff(statBuffSheet[702001]);
            var groupId = buff.RowData.GroupId;
            var drr = stats.DRR;
            stats.AddBuff(buff, buffLimitSheet);
            Assert.Equal(drr + buffDrrValue, stats.DRR);

            // reset stats
            stats.RemoveBuff(buff);
            Assert.Equal(drr, stats.DRR);
            // +60% atk but limit 50% stats
            buffLimitSheet.Set($"group_id,modify_type,percentage\n{groupId},Add,{buffLimitValue}");
            var modifier = buffLimitSheet[groupId].GetModifier(buff.RowData.StatType);
            stats.AddBuff(buff, buffLimitSheet);
            Assert.Equal(drr + buffLimitValue, modifier.GetModifiedAll(drr));
            Assert.Equal(drr + buffLimitValue, stats.DRR);
        }
    }
}
