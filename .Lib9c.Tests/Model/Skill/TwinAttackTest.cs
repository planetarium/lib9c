namespace Lib9c.Tests.Model.Skill
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Crypto;
    using Nekoyume.Battle;
    using Nekoyume.Model;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Xunit;

    public class TwinAttackTest
    {
        private readonly TableSheets _tableSheets = new (TableSheetsImporter.ImportSheets());

        [Theory]
        // Attack count returns to 1 when exceeds max attack(combo) count.
        // lvl. 1 ~ 10 : Max 2 combo
        [InlineData(1, 0, 2, true)]
        [InlineData(1, 0, 2, false)]
        [InlineData(1, 1, 1, true)]
        [InlineData(1, 1, 1, false)]
        [InlineData(1, 2, 2, true)]
        [InlineData(1, 2, 2, false)]
        // lvl. 11 ~ 99 : max 3 combo
        [InlineData(11, 1, 3, true)]
        [InlineData(11, 1, 3, false)]
        [InlineData(11, 2, 1, true)]
        [InlineData(11, 2, 1, false)]
        [InlineData(11, 3, 2, true)]
        [InlineData(11, 3, 2, false)]
        // lvl. 100 ~ 249 : max 4 combo
        [InlineData(100, 2, 4, true)]
        [InlineData(100, 2, 4, false)]
        [InlineData(100, 3, 1, true)]
        [InlineData(100, 3, 1, false)]
        [InlineData(100, 4, 2, true)]
        [InlineData(100, 4, 2, false)]
        // lvl. 250 ~ : max 5 combo
        [InlineData(250, 3, 5, true)]
        [InlineData(250, 3, 5, false)]
        [InlineData(250, 4, 1, true)]
        [InlineData(250, 4, 1, false)]
        [InlineData(250, 5, 2, true)]
        [InlineData(250, 5, 2, false)]
        public void UseTwinAttack(int level, int initialAttackCount, int expectedAttackCount, bool copyCharacter)
        {
            Assert.True(_tableSheets.SkillSheet.TryGetValue(700008, out var skillRow));
            var twinAttack = new TwinAttack(skillRow, 100, 100, default, StatType.NONE);
            var avatarState = new AvatarState(
                new PrivateKey().Address,
                new PrivateKey().Address,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                new PrivateKey().Address
            )
            {
                level = level,
            };
            var worldRow = _tableSheets.WorldSheet.First;
            Assert.NotNull(worldRow);

            var simulator = new StageSimulator(
                new TestRandom(),
                avatarState,
                new List<Guid>(),
                null,
                new List<Nekoyume.Model.Skill.Skill>(),
                1,
                1,
                _tableSheets.StageSheet[1],
                _tableSheets.StageWaveSheet[1],
                false,
                20,
                _tableSheets.GetSimulatorSheets(),
                _tableSheets.EnemySkillSheet,
                _tableSheets.CostumeStatSheet,
                StageSimulator.GetWaveRewards(
                    new TestRandom(),
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                copyCharacter
            );
            var player = new Player(avatarState, simulator)
            {
                AttackCount = initialAttackCount,
            };

            var enemyRow = _tableSheets.CharacterSheet.OrderedList
                .FirstOrDefault(e => e.Id > 200000);
            Assert.NotNull(enemyRow);

            var enemy = new Enemy(player, enemyRow, 1);

            player.Targets.Add(enemy);

            Assert.Equal(initialAttackCount, player.AttackCount);
            var battleStatus = twinAttack.Use(player, 0, new List<StatBuff>(), copyCharacter);

            Assert.NotNull(battleStatus);
            Assert.Equal(!copyCharacter, battleStatus.Character is null);
            Assert.Equal(2, battleStatus.SkillInfos.Count());
            Assert.Equal(expectedAttackCount, player.AttackCount);
        }
    }
}
