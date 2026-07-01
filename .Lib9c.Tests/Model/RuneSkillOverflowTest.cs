namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Xunit;

    /// <summary>
    /// Regression test for PLD-1390. A caster-stat-referencing rune skill
    /// (e.g. rune 10003, ATK% Caster) used to throw an <see cref="OverflowException"/>
    /// inside <see cref="Player.SetRuneSkills"/> when <c>stat * SkillValue</c> exceeded
    /// <see cref="int.MaxValue"/>, because the resulting power was cast to <c>int</c>.
    /// The power is now computed as <c>long</c>, matching the rest of the skill/damage
    /// pipeline (<see cref="Nekoyume.Model.Skill.SkillFactory"/>, <c>Skill.Power</c>,
    /// <c>SkillCustomField.BuffValue</c> are all <c>long</c>).
    /// Based on the real failing transaction
    /// 1ab27944f05a195c1d5a1906513bd15e7c2e97253ba077e9bd0272965abf556a.
    /// </summary>
    public class RuneSkillOverflowTest
    {
        private readonly TableSheets _tableSheets;
        private readonly AvatarState _avatarState;

        /// <summary>
        /// Initializes shared table sheets and a default avatar state for the tests.
        /// </summary>
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

        /// <summary>
        /// Verifies that <see cref="Player.SetRuneSkills"/> no longer throws
        /// <see cref="OverflowException"/> for a high-stat caster and that the resulting
        /// rune skill power is kept as the full <c>long</c> value.
        /// </summary>
        [Fact]
        public void SetRuneSkills_DoesNotOverflow_On_High_ATK()
        {
            // Values taken from the real failing tx.
            const long atk = 629_550_973L;
            const int runeId = 10003;
            const int runeLevel = 200;

            // (int)Math.Round(629_550_973 * 4.47m) = 2_814_092_849 > int.MaxValue (used to throw).
            const long expectedPower = 2_814_092_849L;

            var player = new Player(
                _avatarState,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);

            // Inflate the player's ATK to the on-chain value via a collection Add modifier.
            var baseAtk = player.Stats.GetStatAsLong(StatType.ATK);
            player.Stats.SetCollections(new[]
            {
                new StatModifier(StatType.ATK, StatModifier.OperationType.Add, atk - baseAtk),
            });
            Assert.Equal(atk, player.Stats.GetStatAsLong(StatType.ATK));

            // Sanity-check the sheet row matches the on-chain rune option.
            Assert.True(
                _tableSheets.RuneOptionSheet.TryGetOptionInfo(runeId, runeLevel, out var optionInfo));
            Assert.Equal(4.47m, optionInfo.SkillValue);
            Assert.Equal(StatModifier.OperationType.Percentage, optionInfo.SkillValueType);
            Assert.Equal(StatType.ATK, optionInfo.SkillStatType);
            Assert.Equal(StatReferenceType.Caster, optionInfo.StatReferenceType);

            var runeState = new RuneState(runeId, runeLevel);

            // Must not throw, and the power must be the full long value (no int truncation).
            player.SetRuneSkills(
                new List<RuneState> { runeState },
                _tableSheets.RuneOptionSheet,
                _tableSheets.SkillSheet);

            var runeSkill = player.RuneSkills.Single();
            Assert.Equal(expectedPower, runeSkill.Power);
            Assert.True(runeSkill.CustomField.HasValue);
            Assert.Equal(expectedPower, runeSkill.CustomField.Value.BuffValue);
        }

        /// <summary>
        /// Confirms that the conversion used by the fix keeps the full value where a raw
        /// <c>(int)</c> cast of the same decimal would overflow.
        /// </summary>
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
