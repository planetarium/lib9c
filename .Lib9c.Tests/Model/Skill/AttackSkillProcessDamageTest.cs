namespace Lib9c.Tests.Model.Skill
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Crypto;
    using Nekoyume.Battle;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Tests for AttackSkill.ProcessDamage method.
    /// </summary>
    public class AttackSkillProcessDamageTest
    {
        private readonly TableSheets _tableSheets = new (TableSheetsImporter.ImportSheets());

        public AttackSkillProcessDamageTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        [Fact]
        public void ProcessDamage_ShouldBeIdenticalToLegacyProcessDamage()
        {
            // Arrange
            var skillRow = _tableSheets.SkillSheet.Values.First();
            var testSkill = new TestAttackSkill(skillRow, 100, 100, 10000, StatType.ATK);

            var avatar = AvatarState.Create(
                new PrivateKey().Address,
                new PrivateKey().Address,
                0,
                _tableSheets.GetAvatarSheets(),
                new PrivateKey().Address);

            var simulator = new StageSimulator(
                new TestRandom(),
                avatar,
                new List<System.Guid>(),
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
                    new TestRandom(),
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet),
                new List<StatModifier>(),
                _tableSheets.BuffLimitSheet,
                _tableSheets.BuffLinkSheet,
                true);
            var character = new Player(avatar, simulator);
            var enemyRow = _tableSheets.CharacterSheet.OrderedList
                .FirstOrDefault(e => e.Id > 200000);
            Assert.NotNull(enemyRow);
            var enemy = new Enemy(character, enemyRow, 1);
            character.Targets.Add(enemy);

            // Act
            var legacyResult = testSkill.TestLegacyProcessDamage(character, 1, false, true).ToList();
            var currentResult = testSkill.TestProcessDamage(character, 1, false, true).ToList();

            // Assert
            Assert.Equal(legacyResult.Count, currentResult.Count);

            for (int i = 0; i < legacyResult.Count; i++)
            {
                var legacy = legacyResult[i];
                var current = currentResult[i];

                Assert.Equal(legacy.CharacterId, current.CharacterId);
                Assert.Equal(legacy.IsDead, current.IsDead);
                Assert.Equal(legacy.Thorn, current.Thorn);
                Assert.Equal(legacy.Effect, current.Effect);
                Assert.Equal(legacy.Critical, current.Critical);
                Assert.Equal(legacy.SkillCategory, current.SkillCategory);
                Assert.Equal(legacy.WaveTurn, current.WaveTurn);
                Assert.Equal(legacy.ElementalType, current.ElementalType);
                Assert.Equal(legacy.SkillTargetType, current.SkillTargetType);
            }
        }

        /// <summary>
        /// Test implementation of AttackSkill to access protected ProcessDamage method.
        /// </summary>
        private class TestAttackSkill : AttackSkill
        {
            public TestAttackSkill(
                SkillSheet.Row skillRow,
                long power,
                int chance,
                int statPowerRatio,
                StatType referencedStatType)
                : base(skillRow, power, chance, statPowerRatio, referencedStatType)
            {
            }

            // Copy from AttackSkill.ProcessDamage
            public IEnumerable<Nekoyume.Model.BattleStatus.Skill.SkillInfo> TestLegacyProcessDamage(
                CharacterBase caster,
                int simulatorWaveTurn,
                bool isNormalAttack = false,
                bool copyCharacter = true)
            {
                var infos = new List<Nekoyume.Model.BattleStatus.Skill.SkillInfo>();
                var targets = SkillRow.SkillTargetType.GetTarget(caster).ToList();
                var elementalType = SkillRow.ElementalType;

                // Apply stat power ratio
                var powerMultiplier = StatPowerRatio / 10000m;
                var statAdditionalPower = ReferencedStatType != StatType.NONE
                    ? NumberConversionHelper.SafeDecimalToInt32(
                        caster.Stats.GetStat(ReferencedStatType) * powerMultiplier)
                    : default;

                long totalDamage = caster.ATK + Power + statAdditionalPower;
                var multipliers = GetMultiplier(SkillRow.HitCount, 1m);
                for (var i = 0; i < SkillRow.HitCount; i++)
                {
                    var multiplier = multipliers[i];

                    foreach (var target in targets)
                    {
                        if (SkillRow.SkillCategory is SkillCategory.ShatterStrike)
                        {
                            totalDamage = (long)(target.HP * powerMultiplier);
                        }

                        long damage = 0;
                        var isCritical = false;
                        // Skill or when normal attack hit.
                        if (!isNormalAttack ||
                            target.IsHit(caster))
                        {
                            // Apply armor penetration and DEF.
                            var finalDEF = Math.Clamp(target.DEF - caster.ArmorPenetration, 0, int.MaxValue);
                            damage = totalDamage - finalDEF;
                            // Apply multiple hits
                            damage = (long)(damage * multiplier);
                            // Apply damage reduction
                            damage = (long)((damage - target.DRV) *
                                DamageHelper.GetDamageReductionRate(target.DRR));

                            if (damage < 1)
                            {
                                damage = 1;
                            }
                            else
                            {
                                // 모션 배율 적용.
                                damage = caster.GetDamage(
                                    damage,
                                    isNormalAttack || SkillRow.Combo
                                );
                                // 속성 적용.
                                damage = elementalType.GetDamage(target.defElementType, damage);
                                // 치명 적용.
                                isCritical =
                                    SkillRow.SkillCategory is not SkillCategory.ShatterStrike &&
                                    caster.IsCritical(isNormalAttack || SkillRow.Combo);
                                if (isCritical)
                                {
                                    damage = CriticalHelper.GetCriticalDamage(caster, damage);
                                }

                                // double attack must be shown as critical attack
                                isCritical |= SkillRow.SkillCategory is SkillCategory.DoubleAttack;

                                // ShatterStrike has max damage limitation
                                if (SkillRow.SkillCategory is SkillCategory.ShatterStrike)
                                {
                                    damage = Math.Clamp(damage, 1, caster.Simulator.ShatterStrikeMaxDamage);
                                }
                            }

                            target.CurrentHP -= damage;
                        }

                        var iceShield = target.Buffs.Values.OfType<IceShield>().FirstOrDefault();
                        var clone = copyCharacter ? (CharacterBase)target.Clone() : null;
                        infos.Add(new Nekoyume.Model.BattleStatus.Skill.SkillInfo(
                            target.Id,
                            target.IsDead,
                            target.Thorn,
                            damage,
                            isCritical,
                            SkillRow.SkillCategory,
                            simulatorWaveTurn,
                            SkillRow.ElementalType,
                            SkillRow.SkillTargetType,
                            target: clone,
                            iceShield: iceShield));
                    }
                }

                return infos;
            }

            public override Nekoyume.Model.BattleStatus.Skill Use(
                CharacterBase caster,
                int simulatorWaveTurn,
                IEnumerable<Nekoyume.Model.Buff.Buff> buffs,
                bool copyCharacter)
            {
                // Not used in this test
                return null;
            }

            // Expose the actual ProcessDamage method for testing
            public IEnumerable<Nekoyume.Model.BattleStatus.Skill.SkillInfo> TestProcessDamage(
                CharacterBase caster,
                int simulatorWaveTurn,
                bool isNormalAttack = false,
                bool copyCharacter = true)
            {
                return ProcessDamage(caster, simulatorWaveTurn, isNormalAttack, copyCharacter);
            }
        }
    }
}
