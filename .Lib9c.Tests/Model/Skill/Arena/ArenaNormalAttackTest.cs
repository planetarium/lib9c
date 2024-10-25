namespace Lib9c.Tests.Model.Skill.Arena
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Arena;
    using Nekoyume.Model;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Skill.Arena;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Xunit;

    public class ArenaNormalAttackTest
    {
        private readonly TableSheets _tableSheets;
        private readonly AvatarState _avatar1;
        private readonly AvatarState _avatar2;

        private readonly ArenaAvatarState _arenaAvatar1;
        private readonly ArenaAvatarState _arenaAvatar2;

        public ArenaNormalAttackTest()
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

        [Fact]
        public void NormalAttack()
        {
            var simulator = new ArenaSimulator(new TestRandom());
            var myDigest = new ArenaPlayerDigest(_avatar1, _arenaAvatar1);
            var enemyDigest = new ArenaPlayerDigest(_avatar2, _arenaAvatar2);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var challenger =
                new ArenaCharacter(simulator, myDigest, arenaSheets, simulator.HpModifier, new List<StatModifier>());
            var enemy =
                new ArenaCharacter(simulator, enemyDigest, arenaSheets, simulator.HpModifier, new List<StatModifier>());

            var skillRow = _tableSheets.SkillSheet.OrderedList.First(s => s.Id == 100000);
            var skill = new ArenaNormalAttack(skillRow, 100, 100, 0, StatType.NONE);
            var used = skill.Use(challenger, enemy, simulator.Turn, new List<Buff>());
            Assert.Single(used.SkillInfos);
            Assert.Equal(1, challenger.AttackCount);
        }

        [Fact]
        public void FocusSkill()
        {
            const int seed = 10;
            var myDigest = new ArenaPlayerDigest(_avatar1, _arenaAvatar1);
            var enemyDigest = new ArenaPlayerDigest(_avatar2, _arenaAvatar2);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();

            // Without Focus buff
            var simulator = new ArenaSimulator(new TestRandom(seed));
            var challenger =
                new ArenaCharacter(simulator, myDigest, arenaSheets, simulator.HpModifier, new List<StatModifier>());
            var enemy =
                new ArenaCharacter(simulator, enemyDigest, arenaSheets, simulator.HpModifier, new List<StatModifier>());
            var skillRow = _tableSheets.SkillSheet.OrderedList.First(s => s.Id == 100000);
            var skill = new ArenaNormalAttack(skillRow, 100, 100, 0, StatType.NONE);
            var used = skill.Use(challenger, enemy, simulator.Turn, new List<Buff>());
            Assert.Equal(0, challenger.AttackCount);

            // With Focus Buff
            simulator = new ArenaSimulator(new TestRandom(seed));
            challenger = new ArenaCharacter(simulator, myDigest, arenaSheets, simulator.HpModifier, new List<StatModifier>());
            enemy = new ArenaCharacter(simulator, enemyDigest, arenaSheets, simulator.HpModifier, new List<StatModifier>());

            challenger.AddBuff(
                new Focus(
                    _tableSheets.ActionBuffSheet.OrderedList.First(
                        s =>
                            s.ActionBuffType == ActionBuffType.Focus)
                ));
            Assert.Single(challenger.ActionBuffs);

            skill = new ArenaNormalAttack(skillRow, 100, 100, 0, StatType.NONE);
            used = skill.Use(challenger, enemy, simulator.Turn, new List<Buff>());
            Assert.Single(used.SkillInfos);
            Assert.Equal(1, challenger.AttackCount);
        }
    }
}
