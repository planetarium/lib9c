namespace Lib9c.Tests.Model
{
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.Stat;
    using Xunit;

    public class ICharacterStatsTest
    {
        [Fact]
        public void Test()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var stats = new CharacterStats(tableSheets.CharacterSheet[100110], 2);
            // 기본공격 78 + 추가 공격력 3.12 = 81.12
            var currentAtk = stats.atk.Value;
            Assert.Equal(81, currentAtk);
            var statBuff = new AttackBuff(tableSheets.StatBuffSheet[102000]);
            stats.AddBuff(statBuff);
            // 공격력 81 + 버프 25% 20(81 * 0.25) = 101
            Assert.Equal(101, stats.atk.Value);
            var stats2 = new CharacterStats2(tableSheets.CharacterSheet[100110], 2);
            Assert.Equal(8112, stats2.atk.Value);
            stats2.AddBuff(statBuff);
            // 공격력 81.12 + 버프 25% 20.28(81.12 * 0.25) = 101.4
            Assert.Equal(10140, stats2.atk.Value);
        }
    }
}
