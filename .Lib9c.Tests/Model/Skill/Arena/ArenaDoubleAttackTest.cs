namespace Lib9c.Tests.Model.Skill.Arena
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Arena;
    using Lib9c.Model.Buff;
    using Lib9c.Model.Character;
    using Lib9c.Model.Skill.Arena;
    using Lib9c.Model.Stat;
    using Lib9c.Model.State;
    using Lib9c.Tests.Action;
    using Xunit;

    public class ArenaDoubleAttackTest
    {
        private readonly TableSheets _tableSheets;
        private readonly AvatarState _avatar1;
        private readonly AvatarState _avatar2;
        private readonly ArenaAvatarState _arenaAvatar1;
        private readonly ArenaAvatarState _arenaAvatar2;

        public ArenaDoubleAttackTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _avatar1 = AvatarState.Create(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            _avatar2 = AvatarState.Create(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            _arenaAvatar1 = new ArenaAvatarState(_avatar1);
            _arenaAvatar2 = new ArenaAvatarState(_avatar2);
        }

        [Theory]
        [InlineData(100003, 0)]
        [InlineData(700008, 2)]
        public void DoubleAttackTest(int skillId, int expectedAttackCount)
        {
            var simulator = new ArenaSimulator(new TestRandom());
            var myDigest = new ArenaPlayerDigest(_avatar1, _arenaAvatar1);
            var enemyDigest = new ArenaPlayerDigest(_avatar2, _arenaAvatar2);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var challenger =
                new ArenaCharacter(simulator, myDigest, arenaSheets, simulator.HpModifier, new List<StatModifier>());
            var enemy =
                new ArenaCharacter(simulator, enemyDigest, arenaSheets, simulator.HpModifier, new List<StatModifier>());

            var skillRow = _tableSheets.SkillSheet.OrderedList.First(s => s.Id == skillId);
            var skill = new ArenaDoubleAttack(skillRow, 100, 100, 0, StatType.NONE);
            var used = skill.Use(challenger, enemy, simulator.Turn, new List<Buff>());
            Assert.Equal(expectedAttackCount, challenger.AttackCount);
            Assert.Equal(2, used.SkillInfos.Count());
            Assert.True(used.SkillInfos.First().Effect <= used.SkillInfos.Last().Effect);
        }
    }
}
