namespace Lib9c.Tests.Model.Skill.Adventure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Crypto;
    using Nekoyume.Battle;
    using Nekoyume.Model;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class NormalAttackTest
    {
        private readonly TableSheets _tableSheets = new (TableSheetsImporter.ImportSheets());

        public NormalAttackTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Use(bool copyCharacter)
        {
            Assert.True(_tableSheets.SkillSheet.TryGetValue(100000, out var skillRow));
            var normalAttack = new NormalAttack(skillRow, 100, 100, default, StatType.NONE);

            var avatarState = AvatarState.Create(
                new PrivateKey().Address,
                new PrivateKey().Address,
                0,
                _tableSheets.GetAvatarSheets(),
                new PrivateKey().Address);

            var worldRow = _tableSheets.WorldSheet.First;
            Assert.NotNull(worldRow);

            var random = new TestRandom();
            var simulator = new StageSimulator(
                random,
                avatarState,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(BattleType.Adventure),
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
                _tableSheets.BuffLimitSheet,
                _tableSheets.BuffLinkSheet,
                copyCharacter
            );
            var player = new Player(avatarState, simulator);

            var enemyRow = _tableSheets.CharacterSheet.OrderedList
                .FirstOrDefault(e => e.Id > 200000);
            Assert.NotNull(enemyRow);

            var enemy = new Enemy(player, enemyRow, 1);

            player.Targets.Add(enemy);
            var battleStatusSkill = normalAttack.Use(
                player,
                0,
                new List<StatBuff>(),
                copyCharacter);
            Assert.NotNull(battleStatusSkill);
            Assert.Equal(!copyCharacter, battleStatusSkill.Character is null);
            var skillInfo = Assert.Single(battleStatusSkill.SkillInfos);
            Assert.Equal(enemy.Id, skillInfo.CharacterId);
            Assert.Equal(!copyCharacter, skillInfo.Target is null);
        }

        [Fact]
        public void FocusSkill()
        {
            const int seed = 10; // This seed fails to attack enemy with NormalAttack

            // Without Focus buff
            Assert.True(_tableSheets.SkillSheet.TryGetValue(100000, out var skillRow));
            // Set chance to 0 to minimize attack success probability
            var normalAttack = new NormalAttack(skillRow, 100, 0, default, StatType.NONE);

            var avatarState = AvatarState.Create(
                new PrivateKey().Address,
                new PrivateKey().Address,
                0,
                _tableSheets.GetAvatarSheets(),
                new PrivateKey().Address);

            var worldRow = _tableSheets.WorldSheet.First;
            Assert.NotNull(worldRow);

            var simulator = new StageSimulator(
                new TestRandom(seed),
                avatarState,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(BattleType.Adventure),
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
                    new TestRandom(seed),
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                _tableSheets.BuffLimitSheet,
                _tableSheets.BuffLinkSheet
            );
            var player = new Player(avatarState, simulator);

            var enemyRow = _tableSheets.CharacterSheet.OrderedList
                .FirstOrDefault(e => e.Id > 200000);
            Assert.NotNull(enemyRow);

            var enemy = new Enemy(player, enemyRow, 1);

            player.Targets.Add(enemy);
            var battleStatusSkill = normalAttack.Use(
                player,
                0,
                new List<StatBuff>(),
                true
            );
            Assert.NotNull(battleStatusSkill);
            Assert.Equal(0, player.AttackCount);

            // With Focus buff
            avatarState = AvatarState.Create(
                new PrivateKey().Address,
                new PrivateKey().Address,
                0,
                _tableSheets.GetAvatarSheets(),
                new PrivateKey().Address);

            simulator = new StageSimulator(
                new TestRandom(seed),
                avatarState,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(BattleType.Adventure),
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
                    new TestRandom(seed),
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                _tableSheets.BuffLimitSheet,
                _tableSheets.BuffLinkSheet
            );
            player = new Player(avatarState, simulator);
            player.AddBuff(new Focus(_tableSheets.ActionBuffSheet.OrderedList.First(s => s.ActionBuffType == ActionBuffType.Focus)));
            Assert.Single(player.ActionBuffs);

            enemyRow = _tableSheets.CharacterSheet.OrderedList
                .FirstOrDefault(e => e.Id > 200000);
            Assert.NotNull(enemyRow);

            enemy = new Enemy(player, enemyRow, 1);

            player.Targets.Add(enemy);
            battleStatusSkill = normalAttack.Use(
                player,
                0,
                new List<StatBuff>(),
                true
            );
            Assert.NotNull(battleStatusSkill);
            Assert.Equal(1, player.AttackCount);
        }
    }
}
