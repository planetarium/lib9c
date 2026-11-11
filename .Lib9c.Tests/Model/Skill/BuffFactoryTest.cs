namespace Lib9c.Tests.Model.Skill
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Arena;
    using Nekoyume.Model;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Xunit;

    public class BuffFactoryTest
    {
        private readonly TableSheets _tableSheets;
        private readonly AvatarState _avatarState;

        public BuffFactoryTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var gameConfigState = new GameConfigState();
            _avatarState = AvatarState.Create(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                default);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetBuffs_Arena(bool setExtraValueBuffBeforeGetBuffs)
        {
            // Aegis aura atk down
            var skillId = 210012;
            var skillRow = _tableSheets.SkillSheet[skillId];
            var skill = SkillFactory.GetForArena(skillRow, 10, 100, 0, StatType.NONE);
            var simulator = new ArenaSimulator(new TestRandom(), 10);
            var digest = new ArenaPlayerDigest(
                _avatarState,
                new List<Costume>(),
                new List<Equipment>(),
                new AllRuneState(),
                new RuneSlotState(BattleType.Arena)
            );
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var challenger = new ArenaCharacter(
                simulator,
                digest,
                arenaSheets,
                simulator.HpModifier,
                new List<StatModifier>(),
                setExtraValueBuffBeforeGetBuffs: setExtraValueBuffBeforeGetBuffs);
            var buffs = BuffFactory.GetBuffs(
                challenger.Stats,
                skill,
                _tableSheets.SkillBuffSheet,
                _tableSheets.StatBuffSheet,
                _tableSheets.SkillActionBuffSheet,
                _tableSheets.ActionBuffSheet,
                setExtraValueBuffBeforeGetBuffs
            );
            var buff = Assert.IsType<StatBuff>(Assert.Single(buffs));
            Assert.Equal(buff.CustomField is not null, setExtraValueBuffBeforeGetBuffs);
        }

        [Fact]
        public void GetBuffs()
        {
            var player = new Player(
                1,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);
            // Aegis aura atk down
            var skillId = 210012;
            var skillRow = _tableSheets.SkillSheet[skillId];
            var skill = SkillFactory.Get(skillRow, 10, 100, 0, StatType.NONE);
            var buffs = BuffFactory.GetBuffs(
                player.Stats,
                skill,
                _tableSheets.SkillBuffSheet,
                _tableSheets.StatBuffSheet,
                _tableSheets.SkillActionBuffSheet,
                _tableSheets.ActionBuffSheet
            );
            var buff = Assert.IsType<StatBuff>(Assert.Single(buffs));
            Assert.NotNull(buff.CustomField);
        }

        /// <summary>
        /// PLD-1743. 스탯 비례 스탯 버프의 추가값이 int.MaxValue 에서 잘리지 않는다.
        /// </summary>
        [Fact]
        public void GetBuffs_StatReferencedAdditionalValue_BeyondInt32()
        {
            var player = new Player(
                1,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);
            const long targetHp = 100_000_000_000L;
            var baseHp = player.Stats.StatWithoutBuffs.GetStatAsLong(StatType.HP);
            player.Stats.SetCollections(new[]
            {
                new StatModifier(StatType.HP, StatModifier.OperationType.Add, targetHp - baseHp),
            });
            Assert.Equal(targetHp, player.Stats.StatWithoutBuffs.GetStatAsLong(StatType.HP));

            // Thorns, HP 7%
            var skillRow = _tableSheets.SkillSheet[270000];
            var skill = SkillFactory.Get(skillRow, 0, 100, 700, StatType.HP);
            var buffs = BuffFactory.GetBuffs(
                player.Stats,
                skill,
                _tableSheets.SkillBuffSheet,
                _tableSheets.StatBuffSheet,
                _tableSheets.SkillActionBuffSheet,
                _tableSheets.ActionBuffSheet
            );

            var buff = Assert.IsType<StatBuff>(Assert.Single(buffs));
            Assert.NotNull(buff.CustomField);
            const long additional = targetHp * 700 / 10000;
            Assert.True(additional > int.MaxValue);
            Assert.Equal(SkillType.Buff, skillRow.SkillType);
            Assert.Equal(buff.RowData.Value + additional, buff.CustomField.Value.BuffValue);
        }

        /// <summary>
        /// PLD-1743. 상한(BuffLimitSheet)이 없는 그룹의 스탯 비례 버프는 int.MaxValue 를 넘는 값이 스탯까지 그대로 들어간다.
        /// </summary>
        [Fact]
        public void AddBuff_StatReferencedBuff_WithoutLimit_RaisesStatBeyondInt32()
        {
            var player = new Player(
                1,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);
            const long targetDef = 5_000_000_000L;
            var baseDef = player.Stats.StatWithoutBuffs.GetStatAsLong(StatType.DEF);
            player.Stats.SetCollections(new[]
            {
                new StatModifier(StatType.DEF, StatModifier.OperationType.Add, targetDef - baseDef),
            });
            var atkBefore = player.Stats.GetStatAsLong(StatType.ATK);

            // ATK 증가 (DEF 비례 증가) → StatBuff 102021(그룹 102020, BuffLimitSheet 에 없음), DEF 100%
            var skillRow = _tableSheets.SkillSheet[800014];
            Assert.Equal(SkillType.Buff, skillRow.SkillType);
            var skill = SkillFactory.Get(skillRow, 0, 100, 10000, StatType.DEF);
            var buff = Assert.IsType<StatBuff>(Assert.Single(BuffFactory.GetBuffs(
                player.Stats,
                skill,
                _tableSheets.SkillBuffSheet,
                _tableSheets.StatBuffSheet,
                _tableSheets.SkillActionBuffSheet,
                _tableSheets.ActionBuffSheet)));
            Assert.False(_tableSheets.BuffLimitSheet.TryGetValue(buff.RowData.GroupId, out _));
            Assert.Equal(buff.RowData.Value + targetDef, buff.CustomField!.Value.BuffValue);

            player.Stats.AddBuff(buff, _tableSheets.BuffLimitSheet);
            var atkAfter = player.Stats.GetStatAsLong(StatType.ATK);
            Assert.True(atkAfter > int.MaxValue);
            Assert.Equal(atkBefore + buff.RowData.Value + targetDef, atkAfter);
        }

        [Fact]
        public void Thorns()
        {
            var player = new Player(
                1,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);
            // Aegis aura atk down
            var skillId = 270000;
            var skillRow = _tableSheets.SkillSheet[skillId];
            var skill = SkillFactory.Get(skillRow, 0, 100, 700, StatType.HP);
            var buffs = BuffFactory.GetBuffs(
                player.Stats,
                skill,
                _tableSheets.SkillBuffSheet,
                _tableSheets.StatBuffSheet,
                _tableSheets.SkillActionBuffSheet,
                _tableSheets.ActionBuffSheet
            );
            var hp = player.Stats.HP;
            var buff = Assert.IsType<StatBuff>(Assert.Single(buffs));
            Assert.NotNull(buff.CustomField);

            // Equip item
            var equipmentRow =
                _tableSheets.EquipmentItemSheet.Values.Where(r => r.Stat.StatType == StatType.HP).OrderByDescending(r => r.Stat.TotalValue).First();
            var equipment = (Equipment)ItemFactory.CreateItem(equipmentRow, new TestRandom());
            player.Stats.SetEquipments(new[] { equipment, }, _tableSheets.EquipmentItemSetEffectSheet);
            Assert.True(player.Stats.HP > hp);
            buffs = BuffFactory.GetBuffs(
                player.Stats,
                skill,
                _tableSheets.SkillBuffSheet,
                _tableSheets.StatBuffSheet,
                _tableSheets.SkillActionBuffSheet,
                _tableSheets.ActionBuffSheet
            );
            var buff2 = Assert.IsType<StatBuff>(Assert.Single(buffs));
            Assert.NotNull(buff2.CustomField);
            Assert.True(buff2.CustomField.Value.BuffValue > buff.CustomField.Value.BuffValue);
            hp = player.Stats.HP;

            // Arena buff
            player.Stats.IncreaseHpForArena();
            Assert.True(player.Stats.HP > hp);
            buffs = BuffFactory.GetBuffs(
                player.Stats,
                skill,
                _tableSheets.SkillBuffSheet,
                _tableSheets.StatBuffSheet,
                _tableSheets.SkillActionBuffSheet,
                _tableSheets.ActionBuffSheet
            );
            var buff3 = Assert.IsType<StatBuff>(Assert.Single(buffs));
            Assert.NotNull(buff3.CustomField);
            Assert.True(buff3.CustomField.Value.BuffValue > buff2.CustomField.Value.BuffValue);
        }

        [Theory]
        [InlineData(204003, false)]
        [InlineData(206002, true)]
        public void IsDebuff(int buffId, bool hasCustom)
        {
            var player = new Player(
                1,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);
            var skillId = _tableSheets.SkillBuffSheet.Values.First(r => r.BuffIds.Contains(buffId)).SkillId;
            var skillRow = _tableSheets.SkillSheet[skillId];
            var power = hasCustom ? 0 : 100;
            var statPower = hasCustom ? 250 : 0;
            var referencedStat = hasCustom ? StatType.HP : StatType.NONE;
            var skill = SkillFactory.Get(skillRow, power, 100, statPower, referencedStat);
            var buffs = BuffFactory.GetBuffs(
                player.Stats,
                skill,
                _tableSheets.SkillBuffSheet,
                _tableSheets.StatBuffSheet,
                _tableSheets.SkillActionBuffSheet,
                _tableSheets.ActionBuffSheet,
                hasCustom
            );
            var buff = Assert.IsType<StatBuff>(buffs.Single(r => r.BuffInfo.Id == buffId));
            Assert.Equal(buff.CustomField is not null, hasCustom);
            Assert.False(buff.IsBuff());
            Assert.True(buff.IsDebuff());
        }

        [Theory]
        [InlineData(102001, false)]
        [InlineData(102003, true)]
        public void IsBuff(int buffId, bool hasCustom)
        {
            var player = new Player(
                1,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);
            var skillId = _tableSheets.SkillBuffSheet.Values.First(r => r.BuffIds.Contains(buffId)).SkillId;
            var skillRow = _tableSheets.SkillSheet[skillId];
            var power = hasCustom ? 0 : 100;
            var statPower = hasCustom ? 250 : 0;
            var referencedStat = hasCustom ? StatType.ATK : StatType.NONE;
            var skill = SkillFactory.Get(skillRow, power, 100, statPower, referencedStat);
            var buffs = BuffFactory.GetBuffs(
                player.Stats,
                skill,
                _tableSheets.SkillBuffSheet,
                _tableSheets.StatBuffSheet,
                _tableSheets.SkillActionBuffSheet,
                _tableSheets.ActionBuffSheet,
                hasCustom
            );
            var buff = Assert.IsType<StatBuff>(buffs.Single(r => r.BuffInfo.Id == buffId));
            Assert.NotNull(buff.CustomField);
            Assert.True(buff.IsBuff());
            Assert.False(buff.IsDebuff());
        }

        [Fact]
        public void IceShield()
        {
            var player = new Player(
                1,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);
            // IceShield id
            const int skillId = 700012;
            var skillRow = _tableSheets.SkillSheet[skillId];
            var skill = SkillFactory.Get(skillRow, 0, 100, 10000, StatType.ATK);
            var buffs = BuffFactory.GetBuffs(
                player.Stats,
                skill,
                _tableSheets.SkillBuffSheet,
                _tableSheets.StatBuffSheet,
                _tableSheets.SkillActionBuffSheet,
                _tableSheets.ActionBuffSheet
            );
            Assert.Equal(2, buffs.Count);
            var statBuff = Assert.IsType<StatBuff>(buffs.First());
            Assert.NotNull(statBuff.CustomField);
            Assert.Equal(player.ATK + statBuff.RowData.Value, statBuff.CustomField.Value.BuffValue);
            var iceShield = Assert.IsType<IceShield>(buffs.Last());
            var frostBite = iceShield.FrostBite(_tableSheets.StatBuffSheet, _tableSheets.BuffLinkSheet);
            Assert.Null(frostBite.CustomField);
            var power = frostBite.RowData.Value;
            for (var i = 0; i < frostBite.RowData.MaxStack; i++)
            {
                frostBite.SetStack(i);
                var modifier = frostBite.GetModifier();
                Assert.True(modifier.Value < 0);
                Assert.Equal(power * (1 + frostBite.Stack), modifier.Value);
            }
        }
    }
}
