namespace Lib9c.Tests.Model.Skill.Adventure
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class CombatTest
    {
        private readonly TableSheets _tableSheets;
        private readonly AvatarState _avatarState;
        private readonly Player _player;
        private readonly Enemy _enemy;

        public CombatTest()
        {
            var csv = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(csv);

            var gameConfigState = new GameConfigState(csv[nameof(GameConfigSheet)]);
            _avatarState = AvatarState.Create(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                default);

            _player = new Player(
                1,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);
            var simulator = new TestSimulator(
                new TestRandom(),
                _avatarState,
                new List<System.Guid>(),
                _tableSheets.GetSimulatorSheets());
            _player.Simulator = simulator;

            var enemyRow = _tableSheets.CharacterSheet.OrderedList
                .FirstOrDefault(e => e.Id > 200000);
            _enemy = new Enemy(
                _player,
                new CharacterStats(enemyRow, 1),
                enemyRow.Id,
                ElementalType.Normal);
            _enemy.Targets.Add(_player);
        }

        [Theory]
        [InlineData(3, 7, 1000, 110, 90)]
        [InlineData(7, 3, 1000, 110, 90)]
        [InlineData(10, 10, 5000, 120, 50)]
        [InlineData(0, 1000, 0, 999, 1)]
        [InlineData(1000, 0, 0, 999, 1)]
        [InlineData(0, 0, 10000, 300, 57)]
        [InlineData(0, 0, 10000, int.MaxValue, 300)]
        public void CalculateDEFAndDamageReduction(int def, int drv, int drr, int enemyATK, int expectedDamage)
        {
            _player.Stats.SetStatForTest(StatType.DEF, def);
            _player.Stats.SetStatForTest(StatType.DRV, drv);
            _player.Stats.SetStatForTest(StatType.DRR, drr);
            _player.Stats.SetStatForTest(StatType.HIT, 0);
            _enemy.Stats.SetStatForTest(StatType.ATK, enemyATK);
            _enemy.Stats.SetStatForTest(StatType.CRI, 0);

            Assert.True(_tableSheets.SkillSheet.TryGetValue(100000, out var skillRow));
            var normalAttack = new NormalAttack(skillRow, 0, 100, default, StatType.NONE);

            var prevHP = _player.CurrentHP;
            normalAttack.Use(_enemy, 1, new List<StatBuff>(), false);
            var currentHP = _player.CurrentHP;
            var damage = prevHP - currentHP;

            Assert.Equal(expectedDamage, damage);
        }

        [Theory]
        [InlineData(10000, 10, 25)]
        [InlineData(15000, 10, 30)]
        [InlineData(0, 10, 15)]
        [InlineData(-4000, 10, 11)]
        [InlineData(int.MinValue, 10, 10)]
        public void CalculateCritDamage(int cdmg, int atk, int expectedDamage)
        {
            _player.Stats.SetStatForTest(StatType.DEF, 0);
            _player.Stats.SetStatForTest(StatType.HIT, 0);
            _enemy.Stats.SetStatForTest(StatType.ATK, atk);
            _enemy.Stats.SetStatForTest(StatType.CDMG, cdmg);
            _enemy.Stats.SetStatForTest(StatType.CRI, 100);

            Assert.True(_tableSheets.SkillSheet.TryGetValue(100000, out var skillRow));
            var normalAttack = new NormalAttack(skillRow, 0, 100, default, StatType.NONE);

            var prevHP = _player.CurrentHP;
            normalAttack.Use(_enemy, 1, new List<StatBuff>(), false);
            var currentHP = _player.CurrentHP;
            var damage = prevHP - currentHP;

            Assert.Equal(expectedDamage, damage);
        }

        [Fact]
        public void Thorn()
        {
            var prevHP = _enemy.CurrentHP;
            var skill = _enemy.GiveThornDamage(1);
            var currentHP = _enemy.CurrentHP;
            // get 1dmg from thorn
            Assert.Equal(prevHP - 1, currentHP);
            Assert.Equal(prevHP, skill.Character.CurrentHP);
            var skillInfo = Assert.Single(skill.SkillInfos);
            Assert.Equal(currentHP, skillInfo.Target!.CurrentHP);
        }

        [Fact]
        public void Bleed()
        {
            var actionBuffSheet = _tableSheets.ActionBuffSheet;
            var row = actionBuffSheet.Values.First();
            var bleed = Assert.IsType<Bleed>(BuffFactory.GetActionBuff(_enemy.Stats, row));
            var dmg = bleed.Power;
            var prevHP = _player.CurrentHP;
            var skill = bleed.GiveEffect(_player, 1);
            var currentHP = _player.CurrentHP;
            // get dmg from bleed
            Assert.Equal(prevHP - dmg, currentHP);
            Assert.Equal(prevHP, skill.Character.CurrentHP);
            var skillInfo = Assert.Single(skill.SkillInfos);
            Assert.Equal(currentHP, skillInfo.Target!.CurrentHP);
        }

        [Theory]
        [InlineData(700009, 50, 3, new[] { 600001, }, new[] { 600001, 707000, })]
        [InlineData(700009, 100, 0, new[] { 600001, }, new[] { 707000, })]
        [InlineData(700010, 100, 0, new[] { 600001, 704000, }, new[] { 707000, })]
        public void DispelOnUse(int dispelId, int chance, int seed, int[] debuffIdList, int[] expectedResult)
        {
            var simulator = new TestSimulator(
                new TestRandom(seed),
                _avatarState,
                new List<System.Guid>(),
                _tableSheets.GetSimulatorSheets());
            _player.Simulator = simulator;
            var actionBuffSheet = _tableSheets.ActionBuffSheet;

            // Add Debuff
            foreach (var debuffId in debuffIdList)
            {
                var debuff =
                    actionBuffSheet.Values.First(bf => bf.Id == debuffId); // 600001 is bleed
                _player.AddBuff(BuffFactory.GetActionBuff(_player.Stats, debuff));
            }

            Assert.Equal(debuffIdList.Length, _player.Buffs.Count());

            // Use Dispel
            var actionBuffRow = new ActionBuffSheet.Row();
            actionBuffRow.Set(new[] { "707000", "707000", chance.ToString(), "0", "Self", "Dispel", "Normal", "0", });
            var dispelRow = _tableSheets.SkillSheet.Values.First(bf => bf.Id == dispelId);
            var dispel = new BuffSkill(dispelRow, 0, chance, 0, StatType.NONE);
            var battleStatus = dispel.Use(
                _player,
                0,
                new List<Buff>() { new Dispel(actionBuffRow), },
                false);
            Assert.NotNull(battleStatus);
            // Remove Bleed, add Dispel
            Assert.Equal(expectedResult.Length, _player.Buffs.Count);
            Assert.Equal(expectedResult, _player.Buffs.Values.Select(bf => bf.BuffInfo.GroupId).ToArray());
        }

        [Fact]
        public void DispelOnDuration_Block()
        {
            var actionBuffSheet = _tableSheets.ActionBuffSheet;
            var simulator = new TestSimulator(
                new TestRandom(8),
                _avatarState,
                new List<System.Guid>(),
                _tableSheets.GetSimulatorSheets());
            _player.Simulator = simulator;

            // Use Dispel first
            var dispel = actionBuffSheet.Values.First(bf => bf.ActionBuffType == ActionBuffType.Dispel);
            _player.AddBuff(BuffFactory.GetActionBuff(_player.Stats, dispel));
            Assert.Single(_player.Buffs);

            // Use Bleed
            var debuffRow =
                _tableSheets.SkillSheet.Values.First(bf => bf.Id == 600001); // 600001 is bleed
            var debuff = new BuffSkill(debuffRow, 100, 100, 0, StatType.NONE);
            var battleStatus = debuff.Use(
                _enemy,
                0,
                BuffFactory.GetBuffs(
                    _enemy.Stats,
                    debuff,
                    _tableSheets.SkillBuffSheet,
                    _tableSheets.StatBuffSheet,
                    _tableSheets.SkillActionBuffSheet,
                    _tableSheets.ActionBuffSheet
                ),
                false);

            // Bleed should be blocked
            Assert.NotNull(battleStatus);
            // Remove Bleed, add Dispel
            Assert.Single(_player.Buffs);
            Assert.False(battleStatus.SkillInfos.First().Affected);
        }

        [Fact]
        public void DispelOnDuration_Affect()
        {
            var actionBuffSheet = _tableSheets.ActionBuffSheet;

            // Use Dispel first
            var dispel = actionBuffSheet.Values.First(bf => bf.ActionBuffType == ActionBuffType.Dispel);
            _player.AddBuff(BuffFactory.GetActionBuff(_player.Stats, dispel));
            Assert.Single(_player.Buffs);

            // Use Focus
            var buffRow =
                _tableSheets.SkillSheet.Values.First(bf => bf.Id == 700007); // 700007 is Focus
            var buff = new BuffSkill(buffRow, 100, 100, 0, StatType.NONE);
            var battleStatus = buff.Use(
                _player,
                0,
                BuffFactory.GetBuffs(
                    _player.Stats,
                    buff,
                    _tableSheets.SkillBuffSheet,
                    _tableSheets.StatBuffSheet,
                    _tableSheets.SkillActionBuffSheet,
                    _tableSheets.ActionBuffSheet
                ),
                false);

            // Bleed should be blocked
            Assert.NotNull(battleStatus);
            // Add Focus without block
            Assert.Equal(2, _player.Buffs.Count);
            Assert.True(battleStatus.SkillInfos.First().Affected);
        }

        [Fact]
        public void DispelOnDuration_Nothing()
        {
            var actionBuffSheet = _tableSheets.ActionBuffSheet;

            // Use Dispel first
            var dispel = actionBuffSheet.Values.First(bf => bf.ActionBuffType == ActionBuffType.Dispel);
            _player.AddBuff(BuffFactory.GetActionBuff(_player.Stats, dispel));
            Assert.Single(_player.Buffs);

            // Add Bleed
            var bleed = actionBuffSheet.Values.First(bf => bf.Id == 600001);
            _player.AddBuff(BuffFactory.GetActionBuff(_player.Stats, bleed));

            // Attack
            _enemy.Targets.Add(_player);
            var skillRow =
                _tableSheets.SkillSheet.Values.First(bf => bf.Id == 100000);
            var attack = new NormalAttack(skillRow, 100, 100, default, StatType.NONE);
            var battleStatus = attack.Use(
                _enemy,
                0,
                BuffFactory.GetBuffs(
                    _enemy.Stats,
                    attack,
                    _tableSheets.SkillBuffSheet,
                    _tableSheets.StatBuffSheet,
                    _tableSheets.SkillActionBuffSheet,
                    _tableSheets.ActionBuffSheet
                ),
                false);

            // keep debuff
            Assert.Equal(2, _player.Buffs.Count);
            Assert.True(battleStatus.SkillInfos.First().Affected);

            // Attack
            _player.Targets.Add(_enemy);
            battleStatus = attack.Use(
                _player,
                0,
                BuffFactory.GetBuffs(
                    _player.Stats,
                    attack,
                    _tableSheets.SkillBuffSheet,
                    _tableSheets.StatBuffSheet,
                    _tableSheets.SkillActionBuffSheet,
                    _tableSheets.ActionBuffSheet
                ),
                false);

            // keep debuff
            Assert.Equal(2, _player.Buffs.Count);
            Assert.True(battleStatus.SkillInfos.First().Affected);
        }

        [Theory]
        // Buff
        [InlineData(SkillType.Buff, true)]
        [InlineData(SkillType.Buff, false)]
        // Heal
        [InlineData(SkillType.Heal, true)]
        [InlineData(SkillType.Heal, false)]
        public void Issue_2027(SkillType skillType, bool copyCharacter)
        {
            var healSkillRow = _tableSheets.SkillSheet.Values.First(r => r.SkillType == skillType);
            var healSkill = SkillFactory.Get(healSkillRow, 1, 100, 0, StatType.NONE);

            // normal skill id.
            var skillRow = _tableSheets.SkillSheet[100000];
            var prevHP = _player.CurrentHP;
            var normalAttack = new NormalAttack(skillRow, prevHP / 2, 100, default, StatType.NONE);
            normalAttack.Use(_enemy, 1, new List<StatBuff>(), copyCharacter);
            Assert.False(_player.IsDead);
            var buffs = BuffFactory.GetBuffs(
                _enemy.Stats,
                healSkill,
                _tableSheets.SkillBuffSheet,
                _tableSheets.StatBuffSheet,
                _tableSheets.SkillActionBuffSheet,
                _tableSheets.ActionBuffSheet
            );
            var skill = healSkill.Use(_player, 1, buffs, copyCharacter);
            while (!_player.IsDead)
            {
                normalAttack.Use(_enemy, 1, new List<StatBuff>(), copyCharacter);
            }

            Assert.True(_player.IsDead);
            var skillInfo = Assert.Single(skill.SkillInfos);
            Assert.Equal(skillType == SkillType.Heal, skillInfo.Buff is null);
            // character alive when use skill
            Assert.False(skillInfo.IsDead);
            // without Clone, target dead because _player also dead
            Assert.Equal(!copyCharacter, skillInfo.Target!.IsDead);
        }
    }
}
