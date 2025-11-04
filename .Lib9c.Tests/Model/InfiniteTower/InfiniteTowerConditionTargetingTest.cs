namespace Lib9c.Tests.Model.InfiniteTower
{
    using System.Collections.Generic;
    using Lib9c.Tests.Action;
    using Libplanet.Crypto;
    using Nekoyume.Battle;
    using Nekoyume.Model;
    using Nekoyume.Model.InfiniteTower;
    using Nekoyume.Model.Quest;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class InfiniteTowerConditionTargetingTest
    {
        private TableSheets _tableSheets = new (TableSheetsImporter.ImportSheets());

        [Fact]
        public void Self_AppliesToPlayer_NotEnemy()
        {
            var cond = MakeCondition(SkillTargetType.Self, StatType.ATK, 10);
            var (sim, player, enemy) = MakeSimulator(new List<InfiniteTowerCondition> { cond });

            var playerAtkBefore = player.Stats.ATK;
            var enemyAtkBefore = enemy.Stats.ATK;

            sim.ApplyConditionsToCharacter(player, isPlayer: true);
            sim.ApplyConditionsToCharacter(enemy, isPlayer: false);

            Assert.True(player.Stats.ATK >= playerAtkBefore + 10);
            Assert.Equal(enemyAtkBefore, enemy.Stats.ATK);
        }

        [Fact]
        public void Enemy_AppliesToEnemy_NotPlayer()
        {
            var cond = MakeCondition(SkillTargetType.Enemy, StatType.ATK, 10);
            var (sim, player, enemy) = MakeSimulator(new List<InfiniteTowerCondition> { cond });

            var playerAtkBefore = player.Stats.ATK;
            var enemyAtkBefore = enemy.Stats.ATK;

            sim.ApplyConditionsToCharacter(player, isPlayer: true);
            sim.ApplyConditionsToCharacter(enemy, isPlayer: false);

            Assert.Equal(playerAtkBefore, player.Stats.ATK);
            Assert.True(enemy.Stats.ATK >= enemyAtkBefore + 10);
        }

        [Fact]
        public void Enemies_AppliesToEnemy_NotPlayer()
        {
            var cond = MakeCondition(SkillTargetType.Enemies, StatType.ATK, 10);
            var (sim, player, enemy) = MakeSimulator(new List<InfiniteTowerCondition> { cond });

            var playerAtkBefore = player.Stats.ATK;
            var enemyAtkBefore = enemy.Stats.ATK;

            sim.ApplyConditionsToCharacter(player, isPlayer: true);
            sim.ApplyConditionsToCharacter(enemy, isPlayer: false);

            Assert.Equal(playerAtkBefore, player.Stats.ATK);
            Assert.True(enemy.Stats.ATK >= enemyAtkBefore + 10);
        }

        private InfiniteTowerCondition MakeCondition(SkillTargetType target, StatType statType, int value)
        {
            var row = new InfiniteTowerConditionSheet.Row();
            // CSV 데이터 형태로 설정 (Set 메서드 사용)
            var fields = new List<string>
            {
                "1", // Id
                ((int)statType).ToString(), // StatType
                ((int)target).ToString(), // TargetType
                "0", // OperationType (Add)
                value.ToString(), // Value
            };
            row.Set(fields);
            return new InfiniteTowerCondition(row);
        }

        private (InfiniteTowerSimulator Sim, Player Player, Enemy Enemy) MakeSimulator(List<InfiniteTowerCondition> conditions)
        {
            var random = new TestRandom();
            var avatar = new AvatarState(
                new Address("0x1234567890123456789012345678901234567890"),
                new Address("0x1234567890123456789012345678901234567890"),
                0L,
                new QuestList(new QuestSheet(), new QuestRewardSheet(), new QuestItemRewardSheet(), new EquipmentItemRecipeSheet(), new EquipmentItemSubRecipeSheet()),
                new WorldInformation(0L, null, false),
                new Address("0x1234567890123456789012345678901234567890"),
                "TestAvatar");
            var sheets = _tableSheets.GetSimulatorSheets();
            var player = new Player(avatar, sheets);

            // 간단한 적 생성
            var charRow = new CharacterSheet.Row();
            charRow.Set(new List<string> { "1000", "S", "0", "100", "50", "30", "10", "90", "70", "12", "0.8", "0.4", "0", "3.6", "2.8", "0.5", "3" });
            var enemyStats = new CharacterStats(charRow, 1);
            var enemy = new Enemy(player, enemyStats, charRow, charRow.ElementalType);

            var sim = new InfiniteTowerSimulator(
                random,
                avatar,
                new List<System.Guid>(),
                new AllRuneState(),
                new RuneSlotState(Nekoyume.Model.EnumType.BattleType.Adventure),
                1,
                1,
                new InfiniteTowerFloorSheet.Row(),
                new List<InfiniteTowerFloorWaveSheet.WaveData>(),
                false,
                0,
                sheets,
                new EnemySkillSheet(),
                new CostumeStatSheet(),
                new ItemSheet(),
                new List<StatModifier>(),
                new BuffLimitSheet(),
                new BuffLinkSheet(),
                conditions,
                0);
            return (sim, player, enemy);
        }
    }
}
