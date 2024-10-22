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
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Quest;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
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
            _avatarState = AvatarState.Create(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                _tableSheets.DeBuffLimitSheet,
                _tableSheets.BuffLinkSheet
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                _tableSheets.DeBuffLimitSheet,
                _tableSheets.BuffLinkSheet
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                _tableSheets.DeBuffLimitSheet,
                _tableSheets.BuffLinkSheet
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                new DeBuffLimitSheet(),
                _tableSheets.BuffLinkSheet
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                new DeBuffLimitSheet(),
                _tableSheets.BuffLinkSheet
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

            Assert.Equal(row.Stat, player.Stats.CostumeStats.ATK);

            var copy = (Player)player.Clone();

            Assert.Equal(row.Stat, copy.Stats.CostumeStats.ATK);
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
            for (var i = 0; i < count; ++i)
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                _tableSheets.DeBuffLimitSheet,
                _tableSheets.BuffLinkSheet
            );
            var player = simulator.Player;
            Assert.Empty(player.eventMap);
            Assert.Empty(simulator.Log);
            long requiredExp = 0;
            for (var i = player.Level; i < nextLevel; ++i)
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                _tableSheets.DeBuffLimitSheet,
                _tableSheets.BuffLinkSheet
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
            for (var i = 0; i < logCount; i++)
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                _tableSheets.DeBuffLimitSheet,
                _tableSheets.BuffLinkSheet
            );
            var skill = SkillFactory.Get(_tableSheets.SkillSheet[700004], 0, 100, 0, StatType.NONE);
            skill.CustomField = new SkillCustomField { BuffDuration = 2, };
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
            for (var i = 0; i < logCount; i++)
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                _tableSheets.DeBuffLimitSheet,
                _tableSheets.BuffLinkSheet
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
                new SkillCustomField { BuffDuration = duration, BuffValue = percent, },
                actionBuffSheet[705000]);
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
            for (var i = 0; i < logCount; i++)
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
        public void StatsLayerTest()
        {
            var row = _tableSheets.EquipmentItemSheet.Values.First(
                r =>
                    r.Stat.StatType == StatType.ATK);
            var equipment = (Equipment)ItemFactory.CreateItem(_tableSheets.ItemSheet[row.Id], new TestRandom());
            equipment.equipped = true;
            _avatarState.inventory.AddItem(equipment);
            var costumeStatRow =
                _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costumeId = costumeStatRow.CostumeId;
            var costume = ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet[costumeId], Guid.NewGuid());
            // costume.equipped = true;
            // _avatarState.inventory.AddItem(costume);
            var foodRow =
                _tableSheets.ConsumableItemSheet.Values.First(
                    r =>
                        r.Stats.Any(s => s.StatType == StatType.ATK));
            var food = (Consumable)ItemFactory.CreateItem(foodRow, _random);
            _avatarState.inventory.AddItem(food);
            var runeId = 10002;
            var runeState = new RuneState(runeId);
            runeState.LevelUp();
            Assert.Equal(1, runeState.Level);
            var runeStates = new AllRuneState();
            runeStates.AddRuneState(runeState);

            var simulator = new StageSimulator(
                _random,
                _avatarState,
                new List<Guid>()
                {
                    food.ItemId,
                },
                runeStates,
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
                new CostumeStatSheet(),
                new List<ItemBase>(),
                new List<StatModifier>(),
                _tableSheets.DeBuffLimitSheet,
                _tableSheets.BuffLinkSheet
            );
            var player = simulator.Player;
            var costumeLayerAtk = player.ATK;

            // Update collection stat
            var modifiers = new List<StatModifier>();
            var addModifier = new StatModifier(StatType.ATK, StatModifier.OperationType.Add, 100);
            modifiers.Add(new StatModifier(StatType.ATK, StatModifier.OperationType.Percentage, 100));
            modifiers.Add(addModifier);
            modifiers.Add(new StatModifier(StatType.ATK, StatModifier.OperationType.Percentage, -100));
            player.Stats.SetCollections(modifiers);
            Assert.Equal(player.ATK, costumeLayerAtk + addModifier.Value);
            // CollectionStats 100
            // Assert.Equal(2203, player.ATK);
            var collectionLayerAtk = player.ATK;

            // Update stage buff stats
            var stageBuffSkill = CrystalRandomSkillState.GetSkill(
                1,
                _tableSheets.CrystalRandomBuffSheet,
                _tableSheets.SkillSheet);
            var stageBuffs = BuffFactory.GetBuffs(
                player.Stats,
                stageBuffSkill,
                _tableSheets.SkillBuffSheet,
                _tableSheets.StatBuffSheet,
                _tableSheets.SkillActionBuffSheet,
                _tableSheets.ActionBuffSheet
            );
            var statBuffs = new List<StatBuff>();

            foreach (var stageBuff in stageBuffs)
            {
                player.AddBuff(stageBuff);
                if (stageBuff is StatBuff s)
                {
                    statBuffs.Add(s);
                }
            }

            var stageAtkBuff = statBuffs.Single(s => s.GetModifier().StatType == StatType.ATK);
            var stageModifier = stageAtkBuff.GetModifier();
            var stageBuffAtk = (long)stageModifier.GetModifiedValue(collectionLayerAtk);
            // StageBuffStats 1101(50%)
            // Assert.Equal(1101, stageBuffAtk);
            Assert.Equal(player.ATK, collectionLayerAtk + stageBuffAtk);
            // Assert.Equal(3304, player.ATK);

            // Update skill buff stats
            var percentageBuffRow = _tableSheets.StatBuffSheet.Values.First(
                r =>
                    r.StatType == StatType.ATK &&
                    r.OperationType == StatModifier.OperationType.Percentage);
            var percentageBuff = new StatBuff(percentageBuffRow);
            statBuffs.Add(percentageBuff);
            var percentageModifier = percentageBuff.GetModifier();
            var totalBuffPercentageModifier = new StatModifier(StatType.ATK, StatModifier.OperationType.Percentage, stageModifier.Value + percentageModifier.Value);
            var percentageBuffAtk = (long)totalBuffPercentageModifier.GetModifiedValue(costumeLayerAtk + 100);
            // Total PercentageStats 1652(StageBuffStats 50% + BuffStats 25%)
            // Assert.Equal(1652, percentageBuffAtk);

            // Divide buff group id for test
            var addBuffRow = new StatBuffSheet.Row();
            addBuffRow.Set("102003,102003,100,10,Self,ATK,Add,10,true\n".Split(","));

            var addBuff = new StatBuff(addBuffRow);
            statBuffs.Add(addBuff);
            var addBuffModifier = addBuff.GetModifier();
            var addBuffAtk = addBuffModifier.GetModifiedValue(costumeLayerAtk);
            Assert.Equal(10, addBuffAtk);

            player.Stats.SetBuffs(statBuffs, _tableSheets.DeBuffLimitSheet);
            Assert.Equal(player.ATK, collectionLayerAtk + addBuffAtk + percentageBuffAtk);
            // 20 + 1 + 18 + 1829 + 235 + 100 + 1662
            // Assert.Equal(3865, player.ATK);
        }

        [Fact]
        public void IncreaseHpForArena()
        {
            var row = _tableSheets.EquipmentItemSheet.Values.First(
                r =>
                    r.Stat.StatType == StatType.HP);
            var equipment = (Equipment)ItemFactory.CreateItem(_tableSheets.ItemSheet[row.Id], new TestRandom());
            equipment.equipped = true;
            _avatarState.inventory.AddItem(equipment);
            var costumeStatRow =
                _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.HP);
            var costumeId = costumeStatRow.CostumeId;
            var costume = ItemFactory.CreateCostume(_tableSheets.CostumeItemSheet[costumeId], Guid.NewGuid());
            costume.equipped = true;
            _avatarState.inventory.AddItem(costume);
            var foodRow =
                _tableSheets.ConsumableItemSheet.Values.First(
                    r =>
                        r.Stats.Any(s => s.StatType == StatType.HP));
            var food = (Consumable)ItemFactory.CreateItem(foodRow, _random);
            _avatarState.inventory.AddItem(food);

            // Update equipment stats
            var player = new Player(
                _avatarState,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet
            );
            Assert.Equal(player.Stats.BaseHP + player.Stats.EquipmentStats.HP, player.HP);
            // BaseHp 300, EquipmentStats 30
            // Assert.Equal(330, player.HP);
            var equipmentLayerHp = player.HP;

            // Update consumable stats
            player.Use(
                new List<Guid>
                {
                    food.ItemId,
                });
            Assert.Equal(equipmentLayerHp + food.Stats.Where(s => s.StatType == StatType.HP).Sum(s => s.BaseValueAsLong), player.HP);
            // ConsumableStats 29
            // Assert.Equal(359, player.HP);
            var consumableLayerHp = player.HP;

            // Update rune stat
            var runeId = 30001;
            var runeState = new RuneState(runeId);
            runeState.LevelUp();
            Assert.Equal(1, runeState.Level);
            var runeStates = new AllRuneState();
            runeStates.AddRuneState(runeState);
            player.SetRuneStats(runeStates.Runes.Values.ToList(), _tableSheets.RuneOptionSheet, 0);
            var runeOptionRow = _tableSheets.RuneOptionSheet.Values.First(r => r.RuneId == runeId);
            var runeHp = runeOptionRow.LevelOptionMap[1].Stats.Sum(r => r.stat.BaseValueAsLong);
            Assert.Equal(consumableLayerHp + runeHp, player.HP);
            // RuneStats 520
            // Assert.Equal(879, player.HP);
            var runeLayerHp = player.HP;
            Assert.Equal(player.CurrentHP, runeLayerHp);

            // Update costume stats
            player.SetCostumeStat(_tableSheets.CostumeStatSheet);
            Assert.Equal(runeLayerHp + costumeStatRow.Stat, player.HP);
            // CostumeStats 26990
            // Assert.Equal(27869, player.HP);
            var costumeLayerHp = player.HP;
            Assert.Equal(player.CurrentHP, costumeLayerHp);

            // Update collection stat
            var modifiers = new List<StatModifier>();
            var addModifier = new StatModifier(StatType.HP, StatModifier.OperationType.Add, 100);
            modifiers.Add(new StatModifier(StatType.HP, StatModifier.OperationType.Percentage, 200));
            modifiers.Add(addModifier);
            modifiers.Add(new StatModifier(StatType.HP, StatModifier.OperationType.Percentage, -100));
            player.SetCollections(modifiers);
            Assert.Equal(costumeLayerHp + addModifier.Value + costumeLayerHp, player.HP);
            // CollectionStats 100 + 27869(100%)
            // Assert.Equal(55838, player.HP);
            var collectionLayerHp = player.HP;
            Assert.Equal(player.CurrentHP, collectionLayerHp);

            // Arena
            player.Stats.IsArenaCharacter = true;
            player.Stats.IncreaseHpForArena();
            Assert.Equal(collectionLayerHp * 2, player.HP);
            Assert.Equal(player.HP, player.Stats.StatWithoutBuffs.HP);
            // Assert.Equal(111676, player.HP);
            var arenaHp = player.HP;

            var statBuffs = new List<StatBuff>();
            var percentageBuffRow = _tableSheets.StatBuffSheet.Values.First(
                r =>
                    r.StatType == StatType.HP &&
                    r.OperationType == StatModifier.OperationType.Percentage);
            var percentageBuff = new StatBuff(percentageBuffRow);
            statBuffs.Add(percentageBuff);
            var percentageModifier = percentageBuff.GetModifier();
            var percentageBuffAtk = (long)percentageModifier.GetModifiedValue(arenaHp);
            player.Stats.SetBuffs(statBuffs, _tableSheets.DeBuffLimitSheet);
            Assert.Equal(arenaHp + percentageBuffAtk, player.HP);
            Assert.Equal(arenaHp, player.Stats.StatWithoutBuffs.HP);
        }

        [Fact]
        public void IceShield()
        {
            // skill id for ice shield.
            const int skillId = 700012;
            var skill = SkillFactory.GetV1(
                _tableSheets.SkillSheet.Values.First(r => r.Id == skillId),
                100,
                100
            );

            var simulator = new StageSimulator(
                _random,
                _avatarState,
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
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                _tableSheets.DeBuffLimitSheet,
                _tableSheets.BuffLinkSheet
            );
            var player = simulator.Player;
            var enemy = new Enemy(player, _tableSheets.CharacterSheet.Values.First(), 1);
            player.Targets.Add(enemy);
            simulator.Characters = new SimplePriorityQueue<CharacterBase, decimal>();
            simulator.Characters.Enqueue(enemy, 0);
            player.InitAI();
            player.AddSkill(skill);
            var def = player.DEF;
            var spd = enemy.SPD;
            player.Tick();
            Assert.NotEmpty(simulator.Log);
            var log = simulator.Log;
            var e = log.Last();
            var character = e.Character;
            // increase def by ice shield buff
            Assert.True(character.DEF > def);
            enemy.InitAI();
            for (var i = 0; i < 4; i++)
            {
                enemy.Tick();
                e = log.Last();
                character = e.Character;
                // decrease spd by spd debuff
                Assert.True(spd > character.SPD);
                spd = character.SPD;
            }
        }
    }
}
