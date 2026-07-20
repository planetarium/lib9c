namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Action;
    using Nekoyume.Arena;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Xunit;

    /// <summary>
    /// Regression test for PLD-1390. A caster-stat-referencing rune skill
    /// (e.g. rune 10003, ATK% Caster) used to throw an <see cref="OverflowException"/>
    /// inside <see cref="Player.SetRuneSkills"/> when <c>stat * SkillValue</c> exceeded
    /// <see cref="int.MaxValue"/>, because the resulting power was cast to <c>int</c>.
    /// The arena counterpart (<see cref="ArenaCharacter.SetRuneSkills"/>) clamped the same
    /// value to <see cref="int.MaxValue"/> instead. Both now compute the power as
    /// <c>long</c>, matching the rest of the skill/damage pipeline
    /// (<see cref="Nekoyume.Model.Skill.SkillFactory"/>, <c>Skill.Power</c>,
    /// <c>SkillCustomField.BuffValue</c> are all <c>long</c>).
    /// Based on the real failing transaction
    /// 1ab27944f05a195c1d5a1906513bd15e7c2e97253ba077e9bd0272965abf556a.
    /// </summary>
    public class RuneSkillOverflowTest
    {
        private const int RuneId = 10003;
        private const int RuneLevel = 200;

        // Values taken from the real failing tx: ATK 629,550,973, rune 10003 lv200
        // (ATK% Caster, SkillValue 4.47). (int)Math.Round(629_550_973 * 4.47m) =
        // 2,814,092,849 > int.MaxValue.
        private const long Atk = 629_550_973L;
        private const long ExpectedPower = 2_814_092_849L;

        private readonly TableSheets _tableSheets;
        private readonly AvatarState _avatarState;

        public RuneSkillOverflowTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _avatarState = AvatarState.Create(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                default);
        }

        [Fact]
        public void SetRuneSkills_DoesNotOverflow_On_High_ATK()
        {
            var player = new Player(
                _avatarState,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);

            // Inflate the player's ATK to the on-chain value via a collection Add modifier.
            var baseAtk = player.Stats.GetStatAsLong(StatType.ATK);
            player.Stats.SetCollections(new[]
            {
                new StatModifier(StatType.ATK, StatModifier.OperationType.Add, Atk - baseAtk),
            });
            Assert.Equal(Atk, player.Stats.GetStatAsLong(StatType.ATK));

            // Sanity-check the sheet row matches the on-chain rune option.
            Assert.True(
                _tableSheets.RuneOptionSheet.TryGetOptionInfo(RuneId, RuneLevel, out var optionInfo));
            Assert.Equal(4.47m, optionInfo.SkillValue);
            Assert.Equal(StatModifier.OperationType.Percentage, optionInfo.SkillValueType);
            Assert.Equal(StatType.ATK, optionInfo.SkillStatType);
            Assert.Equal(StatReferenceType.Caster, optionInfo.StatReferenceType);

            var runeState = new RuneState(RuneId, RuneLevel);

            // Must not throw, and the power must be the full long value (no int truncation).
            player.SetRuneSkills(
                new List<RuneState> { runeState },
                _tableSheets.RuneOptionSheet,
                _tableSheets.SkillSheet);

            var runeSkill = player.RuneSkills.Single();
            Assert.Equal(ExpectedPower, runeSkill.Power);
            Assert.True(runeSkill.CustomField.HasValue);
            Assert.Equal(ExpectedPower, runeSkill.CustomField.Value.BuffValue);
        }

        [Fact]
        public void ArenaCharacter_SetRuneSkills_KeepsFullLong_On_High_ATK()
        {
            // The arena path used to clamp the power to int.MaxValue via SafeDecimalToInt32;
            // it now keeps the full long value so PvE and arena stay consistent.
            var allRuneState = new AllRuneState();
            allRuneState.AddRuneState(new RuneState(RuneId, RuneLevel));

            // Rune 10003 is a Skill-type rune (rune_type 2) -> Skill slot (index 3).
            var runeSlotState = new RuneSlotState(BattleType.Arena);
            runeSlotState.UpdateSlot(
                new List<RuneSlotInfo> { new RuneSlotInfo(3, RuneId) },
                _tableSheets.RuneListSheet);

            var digest = new ArenaPlayerDigest(
                _avatarState,
                new List<Nekoyume.Model.Item.Costume>(),
                new List<Nekoyume.Model.Item.Equipment>(),
                allRuneState,
                runeSlotState);

            var simulator = new ArenaSimulator(new TestRandom());
            var character = new ArenaCharacter(
                simulator,
                digest,
                _tableSheets.GetArenaSimulatorSheets(),
                simulator.HpModifier,
                new List<StatModifier>
                {
                    new StatModifier(StatType.ATK, StatModifier.OperationType.Add, Atk),
                });

            Assert.True(
                _tableSheets.RuneOptionSheet.TryGetOptionInfo(RuneId, RuneLevel, out var optionInfo));
            var expectedPower = (long)Math.Round(character.ATK * optionInfo.SkillValue);

            var runeSkill = character._runeSkills.Single();
            Assert.Equal(expectedPower, runeSkill.Power);
            Assert.True(runeSkill.Power > int.MaxValue);
        }

        [Fact]
        public void SafeDecimalToInt64_Preserves_Value_Where_Int_Cast_Overflows()
        {
            var rounded = Math.Round(629_550_973L * 4.47m);
            Assert.Equal(2_814_092_849m, rounded);

            // The old code path: an (int) cast of this decimal still overflow-checks and throws.
            Assert.Throws<OverflowException>(() => (int)rounded);

            // The new code path keeps the full value (well within long range).
            Assert.Equal(2_814_092_849L, NumberConversionHelper.SafeDecimalToInt64(rounded));
        }
    }
}
