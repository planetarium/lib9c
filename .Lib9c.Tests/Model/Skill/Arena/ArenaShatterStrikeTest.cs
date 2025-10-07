namespace Lib9c.Tests.Model.Skill.Arena
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Arena;
    using Lib9c.Model.Buff;
    using Lib9c.Model.Character;
    using Lib9c.Model.Skill.Arena;
    using Lib9c.Model.Stat;
    using Lib9c.Model.State;
    using Lib9c.Tests.Action;
    using Xunit;

    public class ArenaShatterStrikeTest
    {
        private readonly TableSheets _tableSheets;
        private readonly AvatarState _avatar1;
        private readonly AvatarState _avatar2;

        private readonly ArenaAvatarState _arenaAvatar1;
        private readonly ArenaAvatarState _arenaAvatar2;

        public ArenaShatterStrikeTest()
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
        // 1bp == 0.01%
        [InlineData(2, 1000, false)]
        [InlineData(2, 3700, false)]
        [InlineData(2, 100_000, true)]
        [InlineData(100_000, 100_000, false)]
        public void Use(int hpModifier, int ratioBp, bool expectedEnemyDead)
        {
            var gameConfigState =
                new GameConfigState((Text)_tableSheets.GameConfigSheet.Serialize());
            var simulator = new ArenaSimulator(
                new TestRandom(),
                hpModifier,
                gameConfigState.ShatterStrikeMaxDamage
            );
            var myDigest = new ArenaPlayerDigest(_avatar1, _arenaAvatar1);
            var enemyDigest = new ArenaPlayerDigest(_avatar2, _arenaAvatar2);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var challenger =
                new ArenaCharacter(
                    simulator,
                    myDigest,
                    arenaSheets,
                    simulator.HpModifier,
                    new List<StatModifier>()
                );
            var enemy =
                new ArenaCharacter(
                    simulator,
                    enemyDigest,
                    arenaSheets,
                    simulator.HpModifier,
                    new List<StatModifier>()
                );

            var skillRow = _tableSheets.SkillSheet.OrderedList.First(s => s.Id == 700010);
            var shatterStrike = new ArenaShatterStrike(skillRow, 0, 0, ratioBp, StatType.NONE);
            var used = shatterStrike.Use(challenger, enemy, simulator.Turn, new List<Buff>());
            Assert.Single(used.SkillInfos);
            Assert.Equal(
                Math.Clamp(
                    enemy.HP * ratioBp / 10000m - enemy.DEF + challenger.ArmorPenetration,
                    1,
                    gameConfigState.ShatterStrikeMaxDamage
                ),
                used.SkillInfos.First().Effect
            );
            Assert.Equal(expectedEnemyDead, enemy.IsDead);
        }
    }
}
