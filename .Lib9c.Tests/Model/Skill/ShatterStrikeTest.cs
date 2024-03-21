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

    public class ShatterStrikeTest
    {
        private readonly TableSheets _tableSheets = new (TableSheetsImporter.ImportSheets());

        [Theory]
        // 1bp == 0.01%
        [InlineData(10000, true)]
        [InlineData(10000, false)]
        [InlineData(1000, true)]
        [InlineData(1000, false)]
        [InlineData(3700, true)]
        [InlineData(3700, false)]
        [InlineData(100000, true)]
        [InlineData(100000, false)]
        public void Use(int ratioBp, bool copyCharacter)
        {
            Assert.True(
                _tableSheets.SkillSheet.TryGetValue(700010, out var skillRow)
            ); // 700011 is ShatterStrike
            var shatterStrike = new ShatterStrike(skillRow, 0, 0, ratioBp, StatType.NONE);

            var avatarState = new AvatarState(
                new PrivateKey().Address,
                new PrivateKey().Address,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                new PrivateKey().Address
            );
            var worldRow = _tableSheets.WorldSheet.First;
            Assert.NotNull(worldRow);

            var random = new TestRandom();
            var simulator = new StageSimulator(
                random,
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
                    random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                copyCharacter
            );
            var player = new Player(avatarState, simulator);
            var enemyRow = _tableSheets.CharacterSheet.OrderedList
                .FirstOrDefault(e => e.Id > 200000);
            Assert.NotNull(enemyRow);

            var enemy = new Enemy(player, enemyRow, 1);

            player.Targets.Add(enemy);
            var used = shatterStrike.Use(player, 0, new List<Buff>(), copyCharacter);
            Assert.NotNull(used);
            var skillInfo = Assert.Single(used.SkillInfos);
            Assert.Equal(
                (long)(enemy.HP * ratioBp / 10000m) - enemy.DEF + player.ArmorPenetration,
                skillInfo.Effect
            );
            if (ratioBp > 10000)
            {
                Assert.True(skillInfo.IsDead);
            }
        }
    }
}
