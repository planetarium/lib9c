namespace Lib9c.Tests.Model.Skill.Arena
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Crypto;
    using Nekoyume.Arena;
    using Nekoyume.Battle;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Skill.Arena;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Tests for ArenaAttackSkill.ProcessDamage method.
    /// </summary>
    public class ArenaAttackSkillProcessDamageTest
    {
        private readonly TableSheets _tableSheets = new (TableSheetsImporter.ImportSheets());
        private readonly AvatarState _avatar1;
        private readonly AvatarState _avatar2;
        private readonly ArenaAvatarState _arenaAvatar1;
        private readonly ArenaAvatarState _arenaAvatar2;

        public ArenaAttackSkillProcessDamageTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _avatar1 = AvatarState.Create(
                new PrivateKey().Address,
                new PrivateKey().Address,
                0,
                _tableSheets.GetAvatarSheets(),
                new PrivateKey().Address);
            _avatar2 = AvatarState.Create(
                new PrivateKey().Address,
                new PrivateKey().Address,
                0,
                _tableSheets.GetAvatarSheets(),
                new PrivateKey().Address);

            _arenaAvatar1 = new ArenaAvatarState(_avatar1);
            _arenaAvatar2 = new ArenaAvatarState(_avatar2);
        }

        [Fact]
        public void ProcessDamage_ShouldBeIdenticalToLegacyProcessDamage()
        {
            // Arrange
            var skillRow = _tableSheets.SkillSheet.Values.First();
            var testSkill = new TestArenaAttackSkill(skillRow, 100, 100, 10000, StatType.ATK);

            var random = new TestRandom();
            var arenaSimulator = new ArenaSimulator(random);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();

            var casterDigest = new ArenaPlayerDigest(_avatar1, new AllRuneState(), new RuneSlotState(BattleType.Arena));
            var targetDigest = new ArenaPlayerDigest(_avatar2, new AllRuneState(), new RuneSlotState(BattleType.Arena));

            var caster = new ArenaCharacter(arenaSimulator, casterDigest, arenaSheets);
            var target = new ArenaCharacter(arenaSimulator, targetDigest, arenaSheets, true);

            // Act
            var legacyResult = testSkill.TestLegacyProcessDamage(caster, target, 1, false).ToList();
            var currentResult = testSkill.TestProcessDamage(caster, target, 1, false).ToList();

            // Assert
            Assert.Equal(legacyResult.Count, currentResult.Count);

            for (int i = 0; i < legacyResult.Count; i++)
            {
                var legacy = legacyResult[i];
                var current = currentResult[i];

                Assert.Equal(legacy.Target.CharacterId, current.Target.CharacterId);
                Assert.Equal(legacy.Effect, current.Effect);
                Assert.Equal(legacy.Critical, current.Critical);
                Assert.Equal(legacy.SkillCategory, current.SkillCategory);
                Assert.Equal(legacy.Turn, current.Turn);
                Assert.Equal(legacy.ElementalType, current.ElementalType);
                Assert.Equal(legacy.SkillTargetType, current.SkillTargetType);
            }
        }

        [Fact]
        public void ProcessDamage_ShouldHandleLargeValuesBetterThanLegacy()
        {
            // Arrange - Create a skill with very high power to test large value handling
            var skillRow = _tableSheets.SkillSheet.Values.First();
            var testSkill = new TestArenaAttackSkill(skillRow, int.MaxValue, 100, 10000, StatType.ATK);

            var random = new TestRandom();
            var arenaSimulator = new ArenaSimulator(random);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();

            var casterDigest = new ArenaPlayerDigest(_avatar1, new AllRuneState(), new RuneSlotState(BattleType.Arena));
            var targetDigest = new ArenaPlayerDigest(_avatar2, new AllRuneState(), new RuneSlotState(BattleType.Arena));

            var caster = new ArenaCharacter(arenaSimulator, casterDigest, arenaSheets);
            caster.Stats.Modify(new[] { new StatModifier(StatType.ATK, StatModifier.OperationType.Add, int.MaxValue) });
            var target = new ArenaCharacter(arenaSimulator, targetDigest, arenaSheets, true);
            target.Stats.Modify(new[] { new StatModifier(StatType.DEF, StatModifier.OperationType.Add, -target.DEF) });

            // Act
            var legacyResult = testSkill.TestLegacyProcessDamage(caster, target, 1, false).ToList();
            var currentResult = testSkill.TestProcessDamage(caster, target, 1, false).ToList();

            // Assert - Current implementation should handle large values better
            Assert.Equal(legacyResult.Count, currentResult.Count);

            for (int i = 0; i < legacyResult.Count; i++)
            {
                var legacy = legacyResult[i];
                var current = currentResult[i];

                // Current implementation should not have overflow issues with large values
                Assert.Equal(int.MaxValue, legacy.Effect);
                Assert.True(current.Effect > int.MaxValue);
            }
        }

        [Fact]
        public void ProcessDamage_ShouldHandleLargeDefenseValuesBetterThanLegacy()
        {
            // Arrange - Test with very high defense values to check DamageHelper.GetFinalDefense changes
            var skillRow = _tableSheets.SkillSheet.Values.First();
            var testSkill = new TestArenaAttackSkill(skillRow, 100, 100, 10000, StatType.ATK);

            var random = new TestRandom();
            var arenaSimulator = new ArenaSimulator(random);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();

            var casterDigest = new ArenaPlayerDigest(_avatar1, new AllRuneState(), new RuneSlotState(BattleType.Arena));
            var targetDigest = new ArenaPlayerDigest(_avatar2, new AllRuneState(), new RuneSlotState(BattleType.Arena));

            var caster = new ArenaCharacter(arenaSimulator, casterDigest, arenaSheets);
            var target = new ArenaCharacter(arenaSimulator, targetDigest, arenaSheets, true);

            // Set very high defense values to test the change from long.MaxValue to DamageHelper.GetFinalDefense
            target.Stats.Modify(new[] { new StatModifier(StatType.DEF, StatModifier.OperationType.Add, int.MaxValue) });

            // Act
            var legacyResult = testSkill.TestLegacyProcessDamage(caster, target, 1, false).ToList();
            var currentResult = testSkill.TestProcessDamage(caster, target, 1, false).ToList();

            // Assert - Current implementation should handle large defense values better
            Assert.Equal(legacyResult.Count, currentResult.Count);

            for (int i = 0; i < legacyResult.Count; i++)
            {
                var legacy = legacyResult[i];
                var current = currentResult[i];

                // Current implementation should handle large defense values without overflow
                Assert.True(current.Effect >= 0, "Current implementation should handle large defense values without overflow");
            }
        }

        [Fact]
        public void ProcessDamage_ShouldHandleLargeDamageReductionValuesBetterThanLegacy()
        {
            // Arrange - Test with very high DRV and DRR values to check DamageHelper.GetReducedDamage changes
            var skillRow = _tableSheets.SkillSheet.Values.First();
            var testSkill = new TestArenaAttackSkill(skillRow, 100, 100, 10000, StatType.ATK);

            var random = new TestRandom();
            var arenaSimulator = new ArenaSimulator(random);
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();

            var casterDigest = new ArenaPlayerDigest(_avatar1, new AllRuneState(), new RuneSlotState(BattleType.Arena));
            var targetDigest = new ArenaPlayerDigest(_avatar2, new AllRuneState(), new RuneSlotState(BattleType.Arena));

            var caster = new ArenaCharacter(arenaSimulator, casterDigest, arenaSheets);
            var target = new ArenaCharacter(arenaSimulator, targetDigest, arenaSheets, true);

            // Set very high DRV and DRR values to test the change in damage reduction calculation
            target.Stats.Modify(new[]
            {
                new StatModifier(StatType.DRV, StatModifier.OperationType.Add, long.MaxValue),
                new StatModifier(StatType.DRR, StatModifier.OperationType.Add, 8100), // Maximum DRR value
            });

            // Act
            var legacyResult = testSkill.TestLegacyProcessDamage(caster, target, 1, false).ToList();
            var currentResult = testSkill.TestProcessDamage(caster, target, 1, false).ToList();

            // Assert - Current implementation should handle large damage reduction values better
            Assert.Equal(legacyResult.Count, currentResult.Count);

            for (int i = 0; i < legacyResult.Count; i++)
            {
                var legacy = legacyResult[i];
                var current = currentResult[i];
                // Current implementation should handle large damage reduction values without overflow
                Assert.True(current.Effect > legacy.Effect);
                Assert.True(current.Effect >= 0, "Current implementation should handle large damage reduction values without overflow");
            }
        }

        /// <summary>
        /// Test implementation of ArenaAttackSkill to access protected ProcessDamage method.
        /// </summary>
        private class TestArenaAttackSkill : ArenaAttackSkill
        {
            public TestArenaAttackSkill(
                SkillSheet.Row skillRow,
                long power,
                int chance,
                int statPowerRatio,
                StatType referencedStatType)
                : base(skillRow, power, chance, statPowerRatio, referencedStatType)
            {
            }

            // Copy from ArenaAttackSkill.ProcessDamage
            public IEnumerable<Nekoyume.Model.BattleStatus.Arena.ArenaSkill.ArenaSkillInfo> TestLegacyProcessDamage(
                ArenaCharacter caster,
                ArenaCharacter target,
                int simulatorWaveTurn,
                bool isNormalAttack = false)
            {
                var infos = new List<Nekoyume.Model.BattleStatus.Arena.ArenaSkill.ArenaSkillInfo>();

                // Apply stat power ratio
                var powerMultiplier = StatPowerRatio / 10000m;
                var statAdditionalPower = ReferencedStatType != StatType.NONE ?
                    NumberConversionHelper.SafeDecimalToInt32(caster.Stats.GetStat(ReferencedStatType) * powerMultiplier) : default;

                var multipliers = GetMultiplier(SkillRow.HitCount, 1m);
                var elementalType = isNormalAttack ? caster.OffensiveElementalType : SkillRow.ElementalType;
                for (var i = 0; i < SkillRow.HitCount; i++)
                {
                    var multiplier = multipliers[i];
                    long damage = 0;
                    var isCritical = false;

                    if (target.IsHit(caster))
                    {
                        damage = (long)(SkillRow.SkillCategory is SkillCategory.ShatterStrike
                            ? target.HP * powerMultiplier
                            : caster.ATK + Power + statAdditionalPower);
                        damage = (long)(damage * multiplier);
                        damage = caster.GetDamage(damage, isNormalAttack || SkillRow.Combo);
                        damage = elementalType.GetDamage(target.DefenseElementalType, damage);
                        isCritical = SkillRow.SkillCategory is not SkillCategory.ShatterStrike &&
                                     caster.IsCritical(isNormalAttack || SkillRow.Combo);
                        if (isCritical)
                        {
                            damage = CriticalHelper.GetCriticalDamageForArena(caster, damage);
                        }

                        // Apply armor penetration and DEF.
                        var finalDEF = Math.Clamp(target.DEF - caster.ArmorPenetration, 0, long.MaxValue);
                        damage = Math.Max(damage - finalDEF, 1);
                        // Apply damage reduce
                        damage = NumberConversionHelper.SafeDecimalToInt32((damage - target.DRV) * DamageHelper.GetDamageReductionRate(target.DRR));

                        // ShatterStrike has max damage limitation
                        if (SkillRow.SkillCategory is SkillCategory.ShatterStrike)
                        {
                            damage = Math.Clamp(damage, 1, caster.Simulator.ShatterStrikeMaxDamage);
                        }

                        target.CurrentHP -= damage;

                        // double attack must be shown as critical attack
                        isCritical |= SkillRow.SkillCategory == SkillCategory.DoubleAttack;
                    }

                    var iceShield = target.Buffs.Values.OfType<IceShield>().FirstOrDefault();
                    infos.Add(new Nekoyume.Model.BattleStatus.Arena.ArenaSkill.ArenaSkillInfo(
                        (ArenaCharacter)target.Clone(),
                        damage,
                        isCritical,
                        SkillRow.SkillCategory,
                        simulatorWaveTurn,
                        elementalType,
                        SkillRow.SkillTargetType,
                        iceShield: iceShield));
                }

                return infos;
            }

            public override Nekoyume.Model.BattleStatus.Arena.ArenaSkill Use(
                ArenaCharacter caster,
                ArenaCharacter target,
                int turn,
                IEnumerable<Buff> buffs)
            {
                // Not used in this test
                return null;
            }

            // Expose the actual ProcessDamage method for testing
            public IEnumerable<Nekoyume.Model.BattleStatus.Arena.ArenaSkill.ArenaSkillInfo> TestProcessDamage(
                ArenaCharacter caster,
                ArenaCharacter target,
                int simulatorWaveTurn,
                bool isNormalAttack = false)
            {
                return ProcessDamage(caster, target, simulatorWaveTurn, isNormalAttack);
            }
        }
    }
}
