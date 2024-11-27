namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Lib9c.Tests.Fixtures.TableCSV;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Arena;
    using Nekoyume.Model;
    using Nekoyume.Model.BattleStatus.Arena;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using Xunit.Abstractions;

    public class ArenaSimulatorTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState1;
        private readonly AvatarState _avatarState2;

        private readonly ArenaAvatarState _arenaAvatarState1;
        private readonly ArenaAvatarState _arenaAvatarState2;

        public ArenaSimulatorTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _random = new TestRandom();

            _avatarState1 = AvatarState.Create(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            _avatarState2 = AvatarState.Create(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            _arenaAvatarState1 = new ArenaAvatarState(_avatarState1);
            _arenaAvatarState2 = new ArenaAvatarState(_avatarState2);
        }

        [Fact]
        public void Simulate()
        {
            var simulator = new ArenaSimulator(_random, 10);
            var myDigest = new ArenaPlayerDigest(_avatarState1, _arenaAvatarState1);
            var enemyDigest = new ArenaPlayerDigest(_avatarState2, _arenaAvatarState2);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var log = simulator.Simulate(
                myDigest,
                enemyDigest,
                arenaSheets,
                new List<StatModifier>
                {
                    new (StatType.ATK, StatModifier.OperationType.Add, 1),
                    new (StatType.HP, StatModifier.OperationType.Add, 100),
                },
                new List<StatModifier>
                {
                    new (StatType.DEF, StatModifier.OperationType.Add, 1),
                    new (StatType.HP, StatModifier.OperationType.Add, 100),
                },
                _tableSheets.BuffLimitSheet,
                _tableSheets.BuffLinkSheet
            );
            var row =
                _tableSheets.CharacterSheet[GameConfig.DefaultAvatarCharacterId];
            var expectedHp = (new CharacterStats(row, myDigest.Level).HP + 100) * simulator.HpModifier;

            Assert.Equal(_random, simulator.Random);

            var turn = log.Events.OfType<ArenaTurnEnd>().Count();
            Assert.Equal(simulator.Turn, turn);

            var players = log.Events.OfType<ArenaSpawnCharacter>();
            var arenaCharacters = new List<ArenaCharacter>();
            foreach (var player in players)
            {
                if (player.Character is ArenaCharacter arenaCharacter)
                {
                    Assert.Equal(expectedHp, arenaCharacter.HP);
                    Assert.Equal(expectedHp, arenaCharacter.CurrentHP);
                    arenaCharacters.Add(arenaCharacter);
                }
            }

            Assert.Equal(2, players.Count());
            Assert.Equal(2, arenaCharacters.Count);
            var challenger = arenaCharacters.Single(a => !a.IsEnemy);
            var enemy = arenaCharacters.Single(a => a.IsEnemy);
            Assert.Equal(enemy.ATK + 1, challenger.ATK);
            Assert.Equal(challenger.DEF + 1, enemy.DEF);

            var dead = log.Events.OfType<ArenaDead>();
            Assert.Single(dead);
            var deadCharacter = dead.First().Character;
            Assert.True(deadCharacter.IsDead);
            Assert.Equal(0, deadCharacter.CurrentHP);
            if (log.Result == ArenaLog.ArenaResult.Win)
            {
                Assert.True(deadCharacter.IsEnemy);
            }
            else
            {
                Assert.False(deadCharacter.IsEnemy);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(5)]
        [InlineData(null)]
        public void HpIncreasingModifier(int? modifier)
        {
            var simulator = modifier.HasValue ? new ArenaSimulator(_random, modifier.Value) : new ArenaSimulator(_random);
            var myDigest = new ArenaPlayerDigest(_avatarState1, _arenaAvatarState1);
            var enemyDigest = new ArenaPlayerDigest(_avatarState2, _arenaAvatarState2);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var log = simulator.Simulate(myDigest, enemyDigest, arenaSheets, new List<StatModifier>(), new List<StatModifier>(), _tableSheets.BuffLimitSheet, _tableSheets.BuffLinkSheet);
            var expectedHpModifier = modifier ?? 2;

            Assert.Equal(_random, simulator.Random);
            Assert.Equal(expectedHpModifier, simulator.HpModifier);

            var turn = log.Events.OfType<ArenaTurnEnd>().Count();
            Assert.Equal(simulator.Turn, turn);

            var players = log.Events
                .OfType<ArenaSpawnCharacter>()
                .Select(p => p.Character)
                .ToList();
            Assert.Equal(2, players.Count);
            foreach (var player in players)
            {
                Assert.Equal(player.Stats.BaseHP * expectedHpModifier, player.CurrentHP);
            }
        }

        [Fact]
        public void TestSpeedModifierBySkill()
        {
            // Unskilled
            var equipmentRow =
                _tableSheets.EquipmentItemSheet.OrderedList.First(e => e.Id == 10114000);

            var item1 = (Equipment)ItemFactory.CreateItem(equipmentRow, new TestRandom());
            Assert.Empty(item1.Skills);
            item1.equipped = true;
            _avatarState1.inventory.AddItem(item1);

            var item2 = (Equipment)ItemFactory.CreateItem(equipmentRow, new TestRandom());
            Assert.Empty(item2.Skills);
            item2.equipped = true;
            _avatarState2.inventory.AddItem(item2);

            var simulator = new ArenaSimulator(new TestRandom(), 10);
            var arenaAvatarState1 = new ArenaAvatarState(_avatarState1);
            arenaAvatarState1.Equipments.Add(item1.ItemId);
            var arenaAvatarState2 = new ArenaAvatarState(_avatarState2);
            arenaAvatarState2.Equipments.Add(item2.ItemId);
            var myDigest = new ArenaPlayerDigest(_avatarState1, arenaAvatarState1);
            var enemyDigest = new ArenaPlayerDigest(_avatarState2, arenaAvatarState2);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var unskilledLog = simulator.Simulate(myDigest, enemyDigest, arenaSheets, new List<StatModifier>(), new List<StatModifier>(), _tableSheets.BuffLimitSheet, _tableSheets.BuffLinkSheet);
            // foreach (var log in unskilledLog)
            // {
            //     _testOutputHelper.WriteLine($"{log.Character.Id} :: {log}");
            // }
            //
            // _testOutputHelper.WriteLine("================================");

            // Skilled
            // Remove Items
            _avatarState1.inventory.Equipments.First().equipped = false;
            _avatarState1.inventory.RemoveNonFungibleItem(item1.ItemId);
            Assert.Empty(_avatarState1.inventory.Equipments);
            _avatarState2.inventory.Equipments.First().equipped = false;
            _avatarState2.inventory.RemoveNonFungibleItem(item2.ItemId);
            Assert.Empty(_avatarState2.inventory.Equipments);

            // Use skilled items
            var skilledItem1 = (Equipment)ItemFactory.CreateItem(equipmentRow, new TestRandom());
            Assert.Empty(skilledItem1.Skills);
            CombinationEquipment.AddSkillOption(
                new AgentState(new PrivateKey().Address),
                skilledItem1,
                new TestRandom(0),
                _tableSheets.EquipmentItemSubRecipeSheetV2.OrderedList!.First(r => r.Id == 10),
                _tableSheets.EquipmentItemOptionSheet,
                _tableSheets.SkillSheet
            );
            Assert.True(skilledItem1.Skills.Any());
            skilledItem1.equipped = true;
            _avatarState1.inventory.AddItem(skilledItem1);
            var skilledItem2 = (Equipment)ItemFactory.CreateItem(equipmentRow, new TestRandom());
            Assert.Empty(skilledItem2.Skills);
            CombinationEquipment.AddSkillOption(
                new AgentState(new PrivateKey().Address),
                skilledItem2,
                new TestRandom(0),
                _tableSheets.EquipmentItemSubRecipeSheetV2.OrderedList!.First(r => r.Id == 10),
                _tableSheets.EquipmentItemOptionSheet,
                _tableSheets.SkillSheet
            );
            Assert.True(skilledItem2.Skills.Any());
            skilledItem2.equipped = true;
            _avatarState2.inventory.AddItem(skilledItem2);

            simulator = new ArenaSimulator(new TestRandom(), 10);
            arenaAvatarState1 = new ArenaAvatarState(_avatarState1);
            arenaAvatarState1.Equipments.Add(skilledItem1.ItemId);
            arenaAvatarState2 = new ArenaAvatarState(_avatarState2);
            arenaAvatarState2.Equipments.Add(skilledItem2.ItemId);
            myDigest = new ArenaPlayerDigest(_avatarState1, arenaAvatarState1);
            enemyDigest = new ArenaPlayerDigest(_avatarState2, arenaAvatarState2);
            arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var skilledLog = simulator.Simulate(myDigest, enemyDigest, arenaSheets, new List<StatModifier>(), new List<StatModifier>(), _tableSheets.BuffLimitSheet, _tableSheets.BuffLinkSheet);
            // foreach (var log in skilledLog)
            // {
            //     _testOutputHelper.WriteLine($"{log.Character.Id} :: {log}");
            // }
        }

        [Fact]
        public void Thorns()
        {
            var random = new TestRandom();
            var equipmentRow =
                _tableSheets.EquipmentItemSheet.Values.First(r => r.Stat.StatType == StatType.HP);
            var skillId = 270000;
            var skillRow = _tableSheets.SkillSheet[skillId];
            var skill = SkillFactory.Get(skillRow, 0, 100, 700, StatType.HP);
            var equipment = (Equipment)ItemFactory.CreateItem(equipmentRow, random);
            equipment.Skills.Add(skill);
            var equipmentRow2 =
                _tableSheets.EquipmentItemSheet.Values.First(r => r.Stat.StatType == StatType.HIT);
            var equipment2 = (Equipment)ItemFactory.CreateItem(equipmentRow2, random);
            equipment2.Skills.Add(skill);
            var avatarState1 = _avatarState1;
            var avatarState2 = _avatarState2;
            avatarState1.inventory.AddItem(equipment);
            avatarState2.inventory.AddItem(equipment2);

            var arenaAvatarState1 = new ArenaAvatarState(avatarState1);
            arenaAvatarState1.UpdateEquipment(
                new List<Guid>
                {
                    equipment.ItemId,
                });
            var arenaAvatarState2 = new ArenaAvatarState(avatarState2);
            arenaAvatarState2.UpdateEquipment(
                new List<Guid>
                {
                    equipment2.ItemId,
                });

            var simulator = new ArenaSimulator(_random);
            var myDigest = new ArenaPlayerDigest(avatarState1, arenaAvatarState1);
            var enemyDigest = new ArenaPlayerDigest(avatarState2, arenaAvatarState2);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var log = simulator.Simulate(myDigest, enemyDigest, arenaSheets, new List<StatModifier>(), new List<StatModifier>(), _tableSheets.BuffLimitSheet, _tableSheets.BuffLinkSheet, true);
            var ticks = log.Events
                .OfType<ArenaTickDamage>()
                .ToList();
            var challengerTick = ticks.First(r => !r.Character.IsEnemy);
            var enemyTick = ticks.First(r => r.Character.IsEnemy);
            Assert.True(challengerTick.Character.HP > enemyTick.Character.HP);
            Assert.True(enemyTick.SkillInfos.First().Effect > challengerTick.SkillInfos.First().Effect);
        }

        [Fact]
        public void Bleed()
        {
            var random = new TestRandom();
            var avatarState1 = _avatarState1;
            var avatarState2 = _avatarState2;

            var characterRow = _tableSheets.CharacterSheet[GameConfig.DefaultAvatarCharacterId];
            var stats = characterRow.ToStats(avatarState1.level);
            const int totalAtk = 141138;
            var baseAtk = stats.ATK;
            var runeOptionSheet = new RuneOptionSheet();
            runeOptionSheet.Set(RuneOptionSheetFixture.Default);
            var runeRow = runeOptionSheet[10003];
            var runes = new AllRuneState(10003, 89);

            var runeSlotState = new RuneSlotState(BattleType.Arena);
            runeSlotState.UpdateSlot(new List<RuneSlotInfo> { new (3, 10003), }, _tableSheets.RuneListSheet);

            const int runeBonus = 896; // Base stat 1777 * 50.424% bonus from RuneLevelBonusSheet
            const int finalAtk = totalAtk + runeBonus;

            var optionInfo = runeRow.LevelOptionMap[89];
            var statModifiers = new List<StatModifier>();
            statModifiers.AddRange(
                optionInfo.Stats.Select(
                    x =>
                        new StatModifier(
                            x.stat.StatType,
                            x.operationType,
                            x.stat.TotalValueAsLong)));
            foreach (var modifier in statModifiers)
            {
                if (modifier.StatType == StatType.ATK)
                {
                    baseAtk += modifier.Value;
                }
            }

            // Add collection modifier without level stats, rune stats
            var collectionModifier = new StatModifier(StatType.ATK, StatModifier.OperationType.Add, totalAtk - baseAtk);
            var modifiers = new List<StatModifier>
            {
                collectionModifier,
                new (StatType.HP, StatModifier.OperationType.Add, totalAtk * 10),
            };

            var simulator = new ArenaSimulator(random);
            var myDigest = new ArenaPlayerDigest(avatarState1, runes, runeSlotState);
            var enemyDigest = new ArenaPlayerDigest(avatarState2, runes, runeSlotState);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets(runeOptionSheet);
            var log = simulator.Simulate(myDigest, enemyDigest, arenaSheets, modifiers, modifiers, _tableSheets.BuffLimitSheet, _tableSheets.BuffLinkSheet, true);
            var spawns = log.Events.OfType<ArenaSpawnCharacter>().ToList();
            Assert.All(spawns, spawn => Assert.Equal(finalAtk, spawn.Character.ATK));
            var ticks = log.Events
                .OfType<ArenaTickDamage>()
                .ToList();
            var challengerTick = ticks.First(r => !r.Character.IsEnemy);
            var enemyTick = ticks.First(r => r.Character.IsEnemy);
            var challengerInfo = challengerTick.SkillInfos.First();
            var enemyInfo = enemyTick.SkillInfos.First();
            var dmg = (int)decimal.Round(finalAtk * optionInfo.SkillValue);
            Assert.Equal(dmg, challengerInfo.Effect);
            Assert.Equal(dmg, enemyInfo.Effect);
        }

        [Fact]
        public void IceShield()
        {
            var random = new TestRandom();
            var avatarState1 = _avatarState1;
            var avatarState2 = _avatarState2;

            var characterRow = _tableSheets.CharacterSheet[GameConfig.DefaultAvatarCharacterId];
            var stats = characterRow.ToStats(avatarState1.level);
            var baseAtk = stats.ATK;
            var runes = new AllRuneState(0);
            var equipmentRow =
                _tableSheets.EquipmentItemSheet.OrderedList.First(e => e.ItemSubType == ItemSubType.Armor);
            var equipment = (Equipment)ItemFactory.CreateItem(equipmentRow, new TestRandom());
            // skill id for ice shield.
            const int skillId = 700011;
            var skill = SkillFactory.GetV1(
                _tableSheets.SkillSheet.Values.First(r => r.Id == skillId),
                100,
                100
            );
            equipment.Skills.Add(skill);
            avatarState1.inventory.AddItem(equipment);
            avatarState2.inventory.AddItem(equipment);
            var runeSlotState = new RuneSlotState(BattleType.Arena);
            var simulator = new ArenaSimulator(random);
            var myDigest = new ArenaPlayerDigest(avatarState1, new List<Costume>(), new List<Equipment> { equipment, }, runes, runeSlotState);
            var enemyDigest = new ArenaPlayerDigest(avatarState2, new List<Costume>(), new List<Equipment> { equipment, }, runes, runeSlotState);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var log = simulator.Simulate(myDigest, enemyDigest, arenaSheets, new List<StatModifier>(), new List<StatModifier>(), _tableSheets.BuffLimitSheet, _tableSheets.BuffLinkSheet, true);
            var spawns = log.Events.OfType<ArenaSpawnCharacter>().ToList();
            Assert.All(spawns, spawn => Assert.Equal(baseAtk, spawn.Character.ATK));
            var ticks = log.Events
                .OfType<ArenaTick>()
                .GroupBy(e => e.Character)
                .ToList();
            var spdMap = new Dictionary<Guid, long>();
            var challenger = spawns.First().Character;
            var enemy = spawns.Last().Character;
            spdMap[challenger.Id] = challenger.SPD;
            spdMap[enemy.Id] = challenger.SPD;
            foreach (var group in ticks)
            {
                var character = group.Key;
                var frostBite = character.StatBuffs.First(b => b.IsDebuff());
                var id = character.Id;
                var spd = character.SPD;
                // decrease spd by spd debuff
                if (frostBite.Stack < 4)
                {
                    Assert.True(spdMap[id] > spd);
                }
                else
                {
                    // don't decrease spd when max stack
                    Assert.Equal(spdMap[id], spd);
                }

                spdMap[id] = spd;
            }
        }
    }
}
