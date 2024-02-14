namespace Lib9c.Tests.Model.Skill
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Arena;
    using Nekoyume.Model;
    using Nekoyume.Model.Buff;
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
            _avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
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
                new List<RuneState>()
            );
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var challenger = new ArenaCharacter(
                simulator,
                digest,
                arenaSheets,
                simulator.HpModifier,
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
                level: 1,
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

        [Fact]
        public void Thorns()
        {
            var player = new Player(
                level: 1,
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
            player.Stats.SetEquipments(new[] { equipment }, _tableSheets.EquipmentItemSetEffectSheet);
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
    }
}
