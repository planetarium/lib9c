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
        [InlineData(100003, 0)]
        [InlineData(700008, 2)]
        public void DoubleAttackTest(int skillId, int expectedAttackCount)
        {
            var simulator = new ArenaSimulator(new TestRandom());
            var myDigest = new ArenaPlayerDigest(_avatar1, _arenaAvatar1);
            var enemyDigest = new ArenaPlayerDigest(_avatar2, _arenaAvatar2);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var challenger =
                new ArenaCharacter(simulator, myDigest, arenaSheets, simulator.HpModifier);
            var enemy =
                new ArenaCharacter(simulator, enemyDigest, arenaSheets, simulator.HpModifier);

            var skillRow = _tableSheets.SkillSheet.OrderedList.First(s => s.Id == skillId);
            var skill = new ArenaDoubleAttack(skillRow, 100, 100, 0, StatType.NONE);
            var used = skill.Use(challenger, enemy, simulator.Turn, new List<Buff>());
            Assert.Equal(expectedAttackCount, challenger._attackCount);
            Assert.Equal(2, used.SkillInfos.Count());
            Assert.True(used.SkillInfos.First().Effect <= used.SkillInfos.Last().Effect);
        }
    }
}
