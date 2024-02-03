namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume;
    using Nekoyume.Battle;
    using Nekoyume.Model;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Quest;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Priority_Queue;
    using Xunit;

    public class PlayerTest
    {
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;

        public PlayerTest()
        {
            _random = new TestRandom();
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
        }

        [Fact]
        public void TickAlive()
        {
            var simulator = new StageSimulator(
                _random,
                _avatarState,
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>()
            );
            var player = simulator.Player;
            var enemy = new Enemy(player, _tableSheets.CharacterSheet.Values.First(), 1);
            player.Targets.Add(enemy);
            player.InitAI();
            player.Tick();

            Assert.NotEmpty(simulator.Log);
            Assert.Equal(nameof(WaveTurnEnd), simulator.Log.Last().GetType().Name);
        }

        [Fact]
        public void TickDead()
        {
            var simulator = new StageSimulator(
                _random,
                _avatarState,
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>()
            );
            var player = simulator.Player;
            var enemy = new Enemy(player, _tableSheets.CharacterSheet.Values.First(), 1);
            player.Targets.Add(enemy);
            player.InitAI();
            player.CurrentHP = -1;

            Assert.True(player.IsDead);

            player.Tick();

            Assert.NotEmpty(simulator.Log);
            Assert.Equal(nameof(WaveTurnEnd), simulator.Log.Last().GetType().Name);
        }

        [Theory]
        [InlineData(SkillCategory.DoubleAttack)]
        [InlineData(SkillCategory.AreaAttack)]
        public void UseDoubleAttack(SkillCategory skillCategory)
        {
            var skill = SkillFactory.GetV1(
                _tableSheets.SkillSheet.Values.First(r => r.SkillCategory == skillCategory),
                100,
                100
            );

            var defaultAttack = SkillFactory.GetV1(
                _tableSheets.SkillSheet.Values.First(r => r.Id == GameConfig.DefaultAttackId),
                100,
                100
            );
            var simulator = new StageSimulator(
                _random,
                _avatarState,
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>()
            );
            var player = simulator.Player;

            var enemy = new Enemy(player, _tableSheets.CharacterSheet.Values.First(), 1)
            {
                CurrentHP = 1,
            };
            player.Targets.Add(enemy);
            simulator.Characters = new SimplePriorityQueue<CharacterBase, decimal>();
            simulator.Characters.Enqueue(enemy, 0);
            player.InitAI();
            player.OverrideSkill(skill);
            player.AddSkill(defaultAttack);
            Assert.Equal(2, player.Skills.Count());

            player.Tick();

            Assert.Single(simulator.Log.OfType<Dead>());
        }

        [Fact]
        public void UseAuraSkill()
        {
            var defaultAttack = SkillFactory.GetV1(
                _tableSheets.SkillSheet.Values.First(r => r.Id == GameConfig.DefaultAttackId),
                100,
                100
            );
            var skillRow = _tableSheets.SkillSheet[210011];
            var skill = SkillFactory.Get(skillRow, 0, 100, 0, StatType.NONE);

            var simulator = new StageSimulator(
                _random,
                _avatarState,
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>()
            );
            var player = simulator.Player;
            var enemy = new Enemy(player, _tableSheets.CharacterSheet.Values.First(), 1);
            player.Targets.Add(enemy);
            simulator.Characters = new SimplePriorityQueue<CharacterBase, decimal>();
            simulator.Characters.Enqueue(enemy, 0);
            player.InitAI();
            player.OverrideSkill(skill);
            player.AddSkill(defaultAttack);
            Assert.Equal(2, player.Skills.Count());
            var prevAtk = player.ATK;
            var prevDef = player.DEF;
            player.Tick();
            Assert.NotEmpty(simulator.Log);
            Assert.Equal(nameof(WaveTurnEnd), simulator.Log.Last().GetType().Name);
            Assert.Equal(prevAtk + prevAtk * 0.3, player.ATK);
            Assert.Equal(prevDef - prevDef * 0.2, player.DEF);
        }

        [Fact]
        public void UseAuraBuffWithFood()
        {
            var defaultAttack = SkillFactory.GetV1(
                _tableSheets.SkillSheet.Values.First(r => r.Id == GameConfig.DefaultAttackId),
                100,
                100
            );
            var skillRow = _tableSheets.SkillSheet[230005];
            var skill = SkillFactory.Get(skillRow, 0, 100, 0, StatType.NONE);
            var foodRow = _tableSheets.ConsumableItemSheet[201000];
            var food = ItemFactory.CreateItemUsable(foodRow, Guid.NewGuid(), 0);
            _avatarState.inventory.AddItem(food);
            var simulator = new StageSimulator(
                _random,
                _avatarState,
                new List<Guid>
                {
                    food.ItemId,
                },
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>()
            );
            var player = simulator.Player;
            var enemy = new Enemy(player, _tableSheets.CharacterSheet.Values.First(), 1);
            player.Targets.Add(enemy);
            simulator.Characters = new SimplePriorityQueue<CharacterBase, decimal>();
            simulator.Characters.Enqueue(enemy, 0);
            player.InitAI();
            player.OverrideSkill(skill);
            player.AddSkill(defaultAttack);
            Assert.Equal(2, player.Skills.Count());
            var prevCri = player.CRI;
            var enemyPrevCri = enemy.CRI;
            player.Tick();
            Assert.NotEmpty(simulator.Log);
            Assert.Equal(nameof(WaveTurnEnd), simulator.Log.Last().GetType().Name);
            // FIXME 0 percent buff not work.
            Assert.Equal(prevCri, player.CRI);
            Assert.Equal(enemyPrevCri / 2, enemy.CRI);
        }

        [Fact]
        public void SetCostumeStat()
        {
            var row = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume = (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[row.CostumeId], _random);
            costume.equipped = true;
            _avatarState.inventory.AddItem(costume);

            var player = new Player(
                _avatarState,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet
            );
            player.SetCostumeStat(_tableSheets.CostumeStatSheet);

            Assert.Equal(row.Stat, player.Stats.OptionalStats.ATK);

            var copy = (Player)player.Clone();

            Assert.Equal(row.Stat, copy.Stats.OptionalStats.ATK);
        }

        [Fact]
        public void GetExp()
        {
            var row = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.HP);
            var costume = (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[row.CostumeId], _random);
            costume.equipped = true;
            _avatarState.inventory.AddItem(costume);

            var player = new Player(
                _avatarState,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet
            );
            var baseHp = player.HP;
            player.SetCostumeStat(_tableSheets.CostumeStatSheet);
            var expectedHp = baseHp + row.Stat;
            Assert.Equal(expectedHp, player.HP);
            Assert.Equal(expectedHp, player.CurrentHP);
            Assert.Equal(1, player.Level);

            player.CurrentHP -= 10;
            var expectedCurrentHp = expectedHp - 10;
            Assert.Equal(expectedCurrentHp, player.CurrentHP);

            var requiredExp = _tableSheets.CharacterLevelSheet[1].ExpNeed;
            player.GetExp2(requiredExp);
            var characterRow = _tableSheets.CharacterSheet[player.CharacterId];
            Assert.Equal(2, player.Level);
            Assert.Equal(expectedHp + characterRow.LvHP, player.HP);
            Assert.Equal(expectedCurrentHp, player.CurrentHP);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 3)]
        [InlineData(5, 5)]
        public void GetExpV2(int level, int count = 1)
        {
            var player = new Player(
                level,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);

            Assert.Empty(player.eventMap);
            for (int i = 0; i < count; ++i)
            {
                var requiredExp = _tableSheets.CharacterLevelSheet[level].ExpNeed;
                player.GetExp3(requiredExp);

                Assert.Equal(level + 1, player.Level);
                ++level;
            }

            Assert.Empty(player.eventMap);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(5, true)]
        [InlineData(3, false)]
        public void GetExpV3(int nextLevel, bool log)
        {
            var simulator = new StageSimulator(
                _random,
                _avatarState,
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>()
            );
            var player = simulator.Player;
            Assert.Empty(player.eventMap);
            Assert.Empty(simulator.Log);
            long requiredExp = 0;
            for (int i = player.Level; i < nextLevel; ++i)
            {
                requiredExp += _tableSheets.CharacterLevelSheet[i + 1].ExpNeed;
            }

            player.GetExp(requiredExp, log);

            if (log)
            {
                Assert.Single(simulator.Log);
                Assert.IsType<GetExp>(simulator.Log.First());
                var getExp = simulator.Log.OfType<GetExp>().First();
                Assert.Equal(requiredExp, getExp.Exp);
            }

            Assert.Equal(nextLevel, player.Level);
            if (nextLevel > 1)
            {
                Assert.Equal(nextLevel - 1, player.eventMap[(int)QuestEventType.Level]);
            }
            else
            {
                Assert.Empty(player.eventMap);
            }
        }

        [Fact]
        public void MaxLevelTest()
        {
            var maxLevel = _tableSheets.CharacterLevelSheet.Max(row => row.Value.Level);
            var player = new Player(
                maxLevel,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);

            var expRow = _tableSheets.CharacterLevelSheet[maxLevel];
            var maxLevelExp = expRow.Exp;
            var requiredExp = expRow.ExpNeed;
            player.GetExp3(requiredExp);

            Assert.Equal(maxLevel, player.Level);
            Assert.Equal(requiredExp - 1, player.Exp.Current - expRow.Exp);
        }

        [Fact]
        public void GetStun()
        {
            var defaultAttack = SkillFactory.GetV1(
                _tableSheets.SkillSheet.Values.First(r => r.Id == GameConfig.DefaultAttackId),
                100,
                100
            );

            var simulator = new StageSimulator(
                _random,
                _avatarState,
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>()
            );
            var player = simulator.Player;
            var enemy = new Enemy(player, _tableSheets.CharacterSheet.Values.First(), 1);
            player.Targets.Add(enemy);
            simulator.Characters = new SimplePriorityQueue<CharacterBase, decimal>();
            simulator.Characters.Enqueue(enemy, 0);
            player.InitAI();
            player.OverrideSkill(defaultAttack);

            var actionBuffSheet = _tableSheets.ActionBuffSheet;
            // force add buff 'Stun'
            // 704000 is ActionBuff id of Stun
            player.AddBuff(BuffFactory.GetActionBuff(player.Stats, actionBuffSheet[704000]));
            var row = actionBuffSheet.Values.First();
            var bleed = BuffFactory.GetActionBuff(enemy.Stats, row);
            player.AddBuff(bleed);
            player.Tick();
            player.Tick();
            Assert.NotEmpty(simulator.Log);
            var log = simulator.Log;
            var logCount = log.Count;
            var logList = log.ToList();
            for (int i = 0; i < logCount; i++)
            {
                var currLog = logList[i];
                if (currLog is Tick)
                {
                    var nextLog = logList[i + 1];

                    // 'Tick' does not give damage
                    Assert.Equal(currLog.Character.Targets.First().CurrentHP, nextLog.Character.Targets.First().CurrentHP);
                    Assert.True(nextLog is TickDamage);
                }
                else if (currLog is TickDamage)
                {
                    var nextLog = logList[i + 1];
                    Assert.True(currLog.Character.CurrentHP > nextLog.Character.CurrentHP);
                }
            }

            Assert.True(logList.Count > 0);
            Assert.Contains(logList, e => e is Tick);
            Assert.Contains(logList, e => e is TickDamage);
            Assert.Contains(logList, e => e is RemoveBuffs);
        }

        [Fact]
        public void GiveStun()
        {
            var simulator = new StageSimulator(
                _random,
                _avatarState,
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>()
            );
            var skill = SkillFactory.Get(_tableSheets.SkillSheet[700004], 0, 100, 0, StatType.NONE);
            skill.CustomField = new SkillCustomField { BuffDuration = 2 };
            var player = simulator.Player;
            var enemy = new Enemy(player, _tableSheets.CharacterSheet.Values.First(), 1);
            player.Targets.Add(enemy);
            simulator.Characters = new SimplePriorityQueue<CharacterBase, decimal>();
            simulator.Characters.Enqueue(enemy, 0);
            player.InitAI();
            enemy.InitAI();
            player.AddSkill(skill);
            player.Tick();
            var actionBuffSheet = _tableSheets.ActionBuffSheet;
            var row = actionBuffSheet.Values.First();
            var bleed = BuffFactory.GetActionBuff(enemy.Stats, row);
            enemy.AddBuff(bleed);
            enemy.Tick();
            enemy.Tick();
            enemy.Tick();
            Assert.NotEmpty(simulator.Log);
            var log = simulator.Log;
            var logCount = log.Count;
            var logList = log.ToList();
            for (int i = 0; i < logCount; i++)
            {
                var currLog = logList[i];
                if (currLog is Tick)
                {
                    var nextLog = logList[i + 1];

                    // 'Tick' does not give damage
                    Assert.Equal(currLog.Character.Targets.First().CurrentHP, nextLog.Character.Targets.First().CurrentHP);
                    Assert.True(nextLog is TickDamage);
                }
                else if (currLog is TickDamage)
                {
                    var nextLog = logList.ElementAtOrDefault(i + 1);
                    if (nextLog != null)
                    {
                        Assert.True(currLog.Character.CurrentHP > nextLog.Character.CurrentHP);
                    }
                }
            }

            Assert.True(logList.Count > 0);
            Assert.Contains(logList, e => e is Tick);
            Assert.Contains(logList, e => e is TickDamage);
            Assert.Contains(logList, e => e is RemoveBuffs);
            Assert.Contains(logList, e => e is Nekoyume.Model.BattleStatus.NormalAttack);
        }

        [Theory]
        [InlineData(1, 100)]
        [InlineData(2, 10)]
        [InlineData(1, 1)]
        public void Vampiric(int duration, int percent)
        {
            var defaultAttack = SkillFactory.GetV1(
                _tableSheets.SkillSheet.Values.First(r => r.Id == GameConfig.DefaultAttackId),
                int.MaxValue / 2,
                100
            );

            var simulator = new StageSimulator(
                _random,
                _avatarState,
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>()
            );
            var player = simulator.Player;
            var enemy = new Enemy(
                player,
                _tableSheets.CharacterSheet.Values.First(),
                1,
                new[] { new StatModifier(StatType.HP, StatModifier.OperationType.Add, int.MaxValue / 2), }
            );
            player.Targets.Add(enemy);
            simulator.Characters = new SimplePriorityQueue<CharacterBase, decimal>();
            simulator.Characters.Enqueue(enemy, 0);
            player.InitAI();
            player.OverrideSkill(defaultAttack);

            var actionBuffSheet = _tableSheets.ActionBuffSheet;
            // force add buff 'Vampiric'
            // 705000 is ActionBuff id of Vampiric
            var vampiric = (Vampiric)BuffFactory.GetCustomActionBuff(
                new SkillCustomField { BuffDuration = duration, BuffValue = percent }, actionBuffSheet[705000]);
            player.AddBuff(vampiric);
            var row = actionBuffSheet.Values.First();
            var bleed = BuffFactory.GetActionBuff(enemy.Stats, row);
            player.AddBuff(bleed);
            player.Tick();
            player.Tick();
            Assert.NotEmpty(simulator.Log);
            var log = simulator.Log;
            var logCount = log.Count;
            var logList = log.ToList();
            for (int i = 0; i < logCount; i++)
            {
                var currLog = logList[i];
                if (currLog is Nekoyume.Model.BattleStatus.NormalAttack)
                {
                    var nextLog = logList[i + 1];
                    if (currLog.Character.ActionBuffs.Any(actionBuff => actionBuff is Vampiric))
                    {
                        Assert.True(nextLog is Tick);
                    }
                    else
                    {
                        Assert.True(nextLog is TickDamage);
                    }
                }
                else if (currLog is Tick healSkill)
                {
                    Assert.Equal(vampiric.RowData.Id, healSkill.SkillId);
                    var healInfo = healSkill.SkillInfos.First();
                    var prevAttack = logList.Take(i).OfType<Nekoyume.Model.BattleStatus.NormalAttack>()
                        .Last();
                    Assert.Equal(
                        (int)(prevAttack.SkillInfos.First().Effect * (vampiric.BasisPoint / 10000m)),
                        healInfo.Effect);
                }
            }

            Assert.True(logList.Count > 0);
            Assert.Contains(logList, e => e is Nekoyume.Model.BattleStatus.NormalAttack);
            Assert.Contains(logList, e => e is TickDamage);
            Assert.Contains(logList, e => e is RemoveBuffs);
            Assert.Contains(logList, e => e is Tick);
        }

        [Fact]
        public void SetCollectionStatsTest()
        {
            var row = _tableSheets.EquipmentItemSheet.Values.First(r => r.Stat.StatType == StatType.HP);
            var costume = (Equipment)ItemFactory.CreateItem(_tableSheets.ItemSheet[row.Id], new TestRandom());
            costume.equipped = true;
            _avatarState.inventory.AddItem(costume);

            var player = new Player(
                _avatarState,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet
            );

            Assert.Equal(row.Stat.BaseValue, player.Stats.EquipmentStats.HP);
            Assert.Equal(player.HP, player.Stats.BaseHP + row.Stat.BaseValue);

            var modifiers = new List<StatModifier>();
            var addModifier = new StatModifier(StatType.HP, StatModifier.OperationType.Add, 100);
            modifiers.Add(new StatModifier(StatType.HP, StatModifier.OperationType.Percentage, 100));
            modifiers.Add(new StatModifier(StatType.HP, StatModifier.OperationType.Percentage, -100));
            modifiers.Add(addModifier);
            player.Stats.SetCollections(modifiers);
            Assert.Equal(player.HP, player.Stats.BaseHP + row.Stat.BaseValue + addModifier.Value);
        }
    }
}
