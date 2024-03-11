namespace Lib9c.Tests.Model.Skill.Arena
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Arena;
    using Nekoyume.Model;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.Skill.Arena;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Xunit;

    public class ArenaCombatTest
    {
        private readonly TableSheets _tableSheets;
        private readonly AvatarState _avatar1;
        private readonly AvatarState _avatar2;

        private readonly ArenaAvatarState _arenaAvatar1;
        private readonly ArenaAvatarState _arenaAvatar2;

        public ArenaCombatTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _avatar1 = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
            _avatar2 = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            _arenaAvatar1 = new ArenaAvatarState(_avatar1);
            _arenaAvatar2 = new ArenaAvatarState(_avatar2);
        }

        [Theory]
        [InlineData(700009, new[] { 600001 })]
        [InlineData(700010, new[] { 600001, 704000 })]
        public void DispelOnUse(int dispelId, int[] debuffIdList)
        {
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var myDigest = new ArenaPlayerDigest(_avatar1, _arenaAvatar1);
            var enemyDigest = new ArenaPlayerDigest(_avatar2, _arenaAvatar2);
            var simulator = new ArenaSimulator(new TestRandom());
            var challenger = new ArenaCharacter(
                simulator,
                myDigest,
                arenaSheets,
                simulator.HpModifier,
                new List<StatModifier>()
            );
            var enemy = new ArenaCharacter(
                simulator,
                enemyDigest,
                arenaSheets,
                simulator.HpModifier,
                new List<StatModifier>()
            );

            // Add Debuff to avatar1
            foreach (var debuffId in debuffIdList)
            {
                var debuffRow = _tableSheets.ActionBuffSheet.Values.First(bf => bf.Id == debuffId);
                challenger.AddBuff(BuffFactory.GetActionBuff(challenger.Stats, debuffRow));
            }

            Assert.Equal(debuffIdList.Length, challenger.Buffs.Count);

            // Use Dispel
            var skillRow = _tableSheets.SkillSheet.Values.First(bf => bf.Id == dispelId);
            var dispelRow = _tableSheets.ActionBuffSheet.Values.First(
                bf => bf.Id == _tableSheets.SkillActionBuffSheet.OrderedList.First(
                        abf => abf.SkillId == dispelId)
                    .BuffIds.First()
            );
            var dispel = new ArenaBuffSkill(skillRow, 0, 100, 0, StatType.NONE);
            dispel.Use(challenger, challenger, simulator.Turn, BuffFactory.GetBuffs(
                challenger.Stats,
                dispel,
                _tableSheets.SkillBuffSheet,
                _tableSheets.StatBuffSheet,
                _tableSheets.SkillActionBuffSheet,
                _tableSheets.ActionBuffSheet
            ));
            Assert.Single(challenger.Buffs);
            Assert.Equal(dispelRow.GroupId, challenger.Buffs.First().Value.BuffInfo.GroupId);
        }
    }
}
