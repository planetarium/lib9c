namespace Lib9c.Tests.Model.Character
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Arena;
    using Nekoyume.Model;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class RemoveAllStatBuffsTest
    {
        private readonly TableSheets _tableSheets;

        public RemoveAllStatBuffsTest()
        {
            _tableSheets = new TableSheets(
                TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void RemoveAllStatBuffs_Removes_All_Positive_Buffs()
        {
            var character = CreateArenaCharacter();

            var buff1 = CreateStatBuff(
                100001, 100001, 50, StatType.ATK);
            var buff2 = CreateStatBuff(
                100002, 100002, 30, StatType.DEF);
            var buff3 = CreateStatBuff(
                100003, 100003, 20, StatType.HP);

            character.AddBuff(buff1);
            character.AddBuff(buff2);
            character.AddBuff(buff3);
            Assert.Equal(3, character.StatBuffs.Count());

            character.RemoveAllStatBuffs();

            Assert.Empty(character.StatBuffs);
        }

        [Fact]
        public void RemoveAllStatBuffs_Preserves_Negative_Debuffs()
        {
            var character = CreateArenaCharacter();

            var positiveBuff = CreateStatBuff(
                100001, 100001, 50, StatType.ATK);
            var negativeBuff = CreateStatBuff(
                200001, 200001, -25, StatType.ATK);

            character.AddBuff(positiveBuff);
            character.AddBuff(negativeBuff);
            Assert.Equal(2, character.StatBuffs.Count());

            character.RemoveAllStatBuffs();

            var remaining = character.StatBuffs.ToList();
            Assert.Single(remaining);
            Assert.True(remaining[0].RowData.Value < 0);
        }

        [Fact]
        public void RemoveAllStatBuffs_NoBuffs_DoesNotThrow()
        {
            var character = CreateArenaCharacter();

            character.RemoveAllStatBuffs();

            Assert.Empty(character.StatBuffs);
        }

        private Nekoyume.Model.ArenaCharacter CreateArenaCharacter()
        {
            var avatar = AvatarState.Create(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                default);
            var simulator = new ArenaSimulator(new TestRandom());
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var digest = new ArenaPlayerDigest(
                avatar,
                new ArenaAvatarState(avatar));

            return new Nekoyume.Model.ArenaCharacter(
                simulator,
                digest,
                arenaSheets,
                simulator.HpModifier,
                new List<StatModifier>());
        }

        private StatBuff CreateStatBuff(
            int id,
            int groupId,
            long value,
            StatType statType,
            int duration = 10)
        {
            var sheet = new StatBuffSheet();
            var csv =
                "id,group,_name,chance,duration,target_type," +
                "stat_type,modify_type,modify_value\n" +
                $"{id},{groupId},test,100,{duration},Self," +
                $"{statType},Percentage,{value}";
            sheet.Set(csv);
            return new StatBuff(sheet[id]);
        }
    }
}
