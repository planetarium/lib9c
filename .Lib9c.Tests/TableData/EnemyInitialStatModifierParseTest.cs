namespace Lib9c.Tests.TableData
{
    using System.Collections.Generic;
    using System.Linq;
    using Nekoyume.Model.Stat;
    using Nekoyume.TableData;
    using Nekoyume.TableData.AdventureBoss;
    using Nekoyume.TableData.Event;
    using Xunit;

    /// <summary>
    /// Enemy initial stat modifiers must be parsed as <see cref="long"/>.
    /// A value above <see cref="int.MaxValue"/> used to fail int parsing and
    /// was silently dropped, making the enemy weaker instead of stronger.
    /// </summary>
    public class EnemyInitialStatModifierParseTest
    {
        private const long OverInt32 = 3_000_000_000L;

        private static readonly Dictionary<string, string> Sheets =
            TableSheetsImporter.ImportSheets();

        // Values within int32 must keep parsing exactly as before (history replay);
        // values above it must no longer be dropped.
        public static IEnumerable<object[]> Values => new[]
        {
            new object[] { 1_610_612_710L },
            new object[] { (long)int.MaxValue },
            new object[] { int.MaxValue + 1L },
            new object[] { OverInt32 },
        };

        [Theory]
        [MemberData(nameof(Values))]
        public void StageSheet(long value)
        {
            var sheet = new StageSheet();
            sheet.Set(WithFirstRowField(nameof(StageSheet), 3, value));

            AssertHpModifier(sheet.First!.EnemyInitialStatModifiers, value);
        }

        [Theory]
        [MemberData(nameof(Values))]
        public void EventDungeonStageSheet(long value)
        {
            var sheet = new EventDungeonStageSheet();
            sheet.Set(WithFirstRowField(nameof(EventDungeonStageSheet), 3, value));

            AssertHpModifier(sheet.First!.EnemyInitialStatModifiers, value);
        }

        [Theory]
        [MemberData(nameof(Values))]
        public void AdventureBossFloorSheet(long value)
        {
            var sheet = new AdventureBossFloorSheet();
            sheet.Set(WithFirstRowField(nameof(AdventureBossFloorSheet), 4, value));

            AssertHpModifier(sheet.First!.EnemyInitialStatModifiers, value);
        }

        [Theory]
        [MemberData(nameof(Values))]
        public void InfiniteTowerFloorSheet(long value)
        {
            var sheet = new InfiniteTowerFloorSheet();
            sheet.Set(WithFirstRowField(nameof(InfiniteTowerFloorSheet), 47, value));

            AssertHpModifier(sheet.First!.EnemyInitialStatModifiers, value);
        }

        [Fact]
        public void ApplyOverInt32ModifierToEnemyStats()
        {
            // Enemies get the modifiers through CharacterStats -> Stats.Modify.
            var characterRow = new TableSheets(Sheets).CharacterSheet.First!;
            var modifiers = new[]
            {
                new StatModifier(StatType.HP, StatModifier.OperationType.Percentage, OverInt32),
            };
            var plain = new CharacterStats(characterRow, 1);
            var modified = new CharacterStats(characterRow, 1, modifiers);

            Assert.True(modified.HP > int.MaxValue);
            Assert.Equal(plain.HP + plain.HP * OverInt32 / 100, modified.HP);
        }

        /// <summary>
        /// Returns the header and the first data row of the sheet, with the
        /// field at <paramref name="index"/> replaced by <paramref name="value"/>.
        /// </summary>
        private static string WithFirstRowField(string sheetName, int index, long value)
        {
            var lines = Sheets[sheetName].Split('\n')
                .Select(line => line.TrimEnd('\r'))
                .ToArray();
            var fields = lines[1].Split(',');
            fields[index] = value.ToString();
            return $"{lines[0]}\n{string.Join(",", fields)}";
        }

        private static void AssertHpModifier(IEnumerable<StatModifier> modifiers, long expected)
        {
            var hp = Assert.Single(modifiers, m => m.StatType == StatType.HP);
            Assert.Equal(StatModifier.OperationType.Percentage, hp.Operation);
            Assert.Equal(expected, hp.Value);
        }
    }
}
