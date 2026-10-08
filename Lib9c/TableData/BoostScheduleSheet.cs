using System;
using System.Collections.Generic;
using System.Linq;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    /// <summary>
    /// Schedules a temporary adjustment of a game value — a reward amount, a cost, a guarantee
    /// count — for a block range, so that an event can be run with <c>patch_table_sheet</c>
    /// instead of rewriting the value's own sheet and patching it back afterwards.
    /// <para>
    /// Columns: <c>id</c>, <c>target</c>, <c>target_id</c>, <c>op</c>, <c>value</c>,
    /// <c>start_block</c>, <c>end_block</c>. A <c>_memo</c> column, like any <c>_</c> prefixed
    /// column, is dropped on load and may be used for notes.
    /// </para>
    /// <para>
    /// <c>target_id</c> is a single id (<c>50</c>), an inclusive range (<c>1~50</c>) or
    /// <c>*</c> for every id. <c>~</c> rather than <c>-</c> keeps a spreadsheet from reading a
    /// range as a date on its way to the CSV.
    /// </para>
    /// <para>
    /// <c>target</c> names what is adjusted and is matched as a plain string by the call site that
    /// owns it, so the sheet itself never needs to know which targets exist. A node that predates a
    /// target simply finds no match for it instead of failing to read the sheet.
    /// <see cref="Targets"/> lists the targets this version applies.
    /// </para>
    /// <para>
    /// A row applies in <c>[start_block, end_block)</c>. When several rows match, the one with the
    /// lowest <c>id</c> wins; rows are never combined, so overlapping rows cannot stack into a
    /// larger adjustment than any single row declares.
    /// </para>
    /// <para>
    /// Reading is total: a row that does not parse is kept but marked invalid and never matches,
    /// so a typo fails toward "no adjustment" instead of aborting every action that reads the
    /// sheet. <see cref="ValidateCsv"/> rejects such a row at patch time so it should not reach
    /// the chain in the first place.
    /// </para>
    /// <para>
    /// Callers must tolerate the sheet being absent — it reaches an existing chain only by
    /// <c>patch_table_sheet</c>. Absence means no adjustment, which is also what keeps
    /// re-evaluation of pre-patch blocks identical.
    /// </para>
    /// </summary>
    [Serializable]
    public class BoostScheduleSheet : Sheet<int, BoostScheduleSheet.Row>
    {
        /// <summary>
        /// The <c>target_id</c> cell that matches every id of a target.
        /// </summary>
        public const string AnyTargetId = "*";

        /// <summary>
        /// Separates the two ends of a <c>target_id</c> range, as in <c>1~50</c>.
        /// </summary>
        public const char TargetIdRangeSeparator = '~';

        /// <summary>
        /// Upper bound of <c>|value|</c>. Keeps every <see cref="Row.Apply"/> inside
        /// <see cref="decimal"/> range so that applying a row can never throw.
        /// </summary>
        public const decimal MaxAbsoluteValue = int.MaxValue;

        /// <summary>
        /// Column names in their expected order, after <c>_</c> prefixed columns are dropped.
        /// </summary>
        public static readonly string[] ColumnNames =
        {
            "id",
            "target",
            "target_id",
            "op",
            "value",
            "start_block",
            "end_block",
        };

        /// <summary>
        /// The targets this version applies. A row naming any other target is valid data but has
        /// no effect until a version that applies it is running.
        /// </summary>
        public static class Targets
        {
            /// <summary>
            /// Guarantee count of an <c>EquipmentSummonSheet</c> group. <c>target_id</c> is the
            /// group id.
            /// </summary>
            public const string EquipmentSummonGuarantee = "EQUIPMENT_SUMMON_GUARANTEE";

            /// <summary>
            /// Guarantee count of a <c>RuneSummonSheet</c> group. <c>target_id</c> is the group id.
            /// </summary>
            public const string RuneSummonGuarantee = "RUNE_SUMMON_GUARANTEE";

            /// <summary>
            /// Guarantee count of a <c>CostumeSummonSheet</c> group. <c>target_id</c> is the
            /// group id.
            /// </summary>
            public const string CostumeSummonGuarantee = "COSTUME_SUMMON_GUARANTEE";

            /// <summary>
            /// Every amount — each drawn rune, crystal, circle — of the
            /// <c>WorldBossBattleRewardSheet</c> reward a <c>Raid</c> grants for its battle.
            /// <c>target_id</c> is the boss id (<c>WorldBossListSheet.boss_id</c>, e.g.
            /// <c>900001</c>), and the block is the one the raid is played in.
            /// </summary>
            public const string WorldBossBattleReward = "WORLD_BOSS_BATTLE_REWARD";

            /// <summary>
            /// Every amount — each drawn rune, crystal, circle — of each
            /// <c>WorldBossKillRewardSheet</c> reward, granted either by the <c>Raid</c> that
            /// raises the boss level or by <c>ClaimWordBossKillReward</c>. <c>target_id</c> is the
            /// boss id (<c>WorldBossListSheet.boss_id</c>), and the block is the one the reward is
            /// granted in, not the one the boss was killed in: a kill left unclaimed until the
            /// event is boosted when it is claimed during the event.
            /// </summary>
            public const string WorldBossKillReward = "WORLD_BOSS_KILL_REWARD";

            /// <summary>
            /// Every amount — each drawn rune, crystal, circle — of each
            /// <c>WorldBossRankRewardSheet</c> reward <c>ClaimRaidReward</c> grants.
            /// <c>target_id</c> is the boss id (<c>WorldBossListSheet.boss_id</c>), and the block
            /// is the one the reward is claimed in, not the one the rank was reached in: a rank
            /// left unclaimed until the event is boosted when it is claimed during the event.
            /// </summary>
            public const string WorldBossRankReward = "WORLD_BOSS_RANK_REWARD";

            /// <summary>
            /// Every target above, for tooling that warns about rows no running code applies.
            /// </summary>
            public static readonly IReadOnlyCollection<string> Applied = new[]
            {
                EquipmentSummonGuarantee,
                RuneSummonGuarantee,
                CostumeSummonGuarantee,
                WorldBossBattleReward,
                WorldBossKillReward,
                WorldBossRankReward,
            };
        }

        /// <summary>
        /// How a row's <see cref="Row.Value"/> is combined with the value it adjusts.
        /// </summary>
        public enum Operation
        {
            /// <summary>
            /// <c>floor(base × value)</c>.
            /// </summary>
            Mul,

            /// <summary>
            /// <c>base + value</c>. <c>value</c> must be an integer.
            /// </summary>
            Add,
        }

        /// <summary>
        /// One scheduled adjustment.
        /// </summary>
        [Serializable]
        public class Row : SheetRow<int>
        {
            /// <inheritdoc />
            public override int Key => Id;

            /// <summary>
            /// The row id. Also the precedence among overlapping rows: lower wins.
            /// </summary>
            public int Id { get; private set; }

            /// <summary>
            /// What this row adjusts. See <see cref="Targets"/>.
            /// </summary>
            public string Target { get; private set; } = string.Empty;

            /// <summary>
            /// First id within <see cref="Target"/> this row applies to, or <c>null</c> for all.
            /// </summary>
            public int? TargetIdBegin { get; private set; }

            /// <summary>
            /// Last id within <see cref="Target"/> this row applies to, inclusive, or
            /// <c>null</c> for all. Equals <see cref="TargetIdBegin"/> for a single id.
            /// </summary>
            public int? TargetIdEnd { get; private set; }

            /// <summary>
            /// How <see cref="Value"/> is combined with the adjusted value.
            /// </summary>
            public Operation Op { get; private set; }

            /// <summary>
            /// The operand of <see cref="Op"/>.
            /// </summary>
            public decimal Value { get; private set; }

            /// <summary>
            /// First block index this row applies to.
            /// </summary>
            public long StartBlockIndex { get; private set; }

            /// <summary>
            /// First block index this row no longer applies to.
            /// </summary>
            public long EndBlockIndex { get; private set; }

            /// <summary>
            /// Whether every cell parsed. An invalid row never matches.
            /// </summary>
            public bool IsValid { get; private set; }

            /// <summary>
            /// Reads one row.
            /// </summary>
            /// <param name="fields">The row's cells, with "_" prefixed columns already dropped.</param>
            /// <remarks>
            /// Total by design: <see cref="Sheet{TKey,TValue}"/> does not guard this call, so a
            /// throw here would fail every action that reads the sheet.
            /// </remarks>
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = Cell(fields, 0) is { } idCell && TryParseInt(idCell, out var id) ? id : 0;
                Target = Cell(fields, 1) ?? string.Empty;
                IsValid = TryParseRest(fields, out var targetIdBegin, out var targetIdEnd,
                    out var op, out var value, out var start, out var end);
                TargetIdBegin = targetIdBegin;
                TargetIdEnd = targetIdEnd;
                Op = op;
                Value = value;
                StartBlockIndex = start;
                EndBlockIndex = end;
                IsValid = IsValid && Id > 0 && Target.Length > 0;
            }

            /// <summary>
            /// Whether this row applies to <paramref name="targetId"/> of
            /// <paramref name="target"/> at <paramref name="blockIndex"/>.
            /// </summary>
            /// <param name="target">The target being adjusted.</param>
            /// <param name="targetId">The id within the target.</param>
            /// <param name="blockIndex">The block being evaluated.</param>
            /// <returns><c>true</c> when the row is valid and covers all three.</returns>
            public bool Matches(string target, int targetId, long blockIndex) =>
                IsValid &&
                string.Equals(Target, target, StringComparison.Ordinal) &&
                (TargetIdBegin is null || (TargetIdBegin <= targetId && targetId <= TargetIdEnd)) &&
                StartBlockIndex <= blockIndex &&
                blockIndex < EndBlockIndex;

            /// <summary>
            /// Applies this row to <paramref name="baseValue"/>, rounding down and clamping the
            /// result to <c>[0, int.MaxValue]</c>.
            /// </summary>
            /// <param name="baseValue">The value before adjustment.</param>
            /// <returns>The adjusted value.</returns>
            /// <remarks>
            /// Rounding down means a small base can be left unchanged by a multiplier — e.g.
            /// <c>1 × 1.5</c> stays <c>1</c>. Use <see cref="Operation.Add"/> when every target
            /// must actually move.
            /// </remarks>
            public int Apply(int baseValue)
            {
                var result = Op == Operation.Add
                    ? baseValue + Value
                    : baseValue * Value;
                result = decimal.Floor(result);
                if (result <= 0m)
                {
                    return 0;
                }

                return result >= int.MaxValue ? int.MaxValue : (int)result;
            }

            private static bool TryParseRest(
                IReadOnlyList<string> fields,
                out int? targetIdBegin,
                out int? targetIdEnd,
                out Operation op,
                out decimal value,
                out long start,
                out long end)
            {
                targetIdBegin = null;
                targetIdEnd = null;
                op = default;
                value = default;
                start = default;
                end = default;

                if (!TryParseTargetIds(Cell(fields, 2), out targetIdBegin, out targetIdEnd) ||
                    !TryParseOperation(Cell(fields, 3), out op) ||
                    !(Cell(fields, 4) is { } valueCell && TryParseDecimal(valueCell, out value)) ||
                    !(Cell(fields, 5) is { } startCell && TryParseLong(startCell, out start)) ||
                    !(Cell(fields, 6) is { } endCell && TryParseLong(endCell, out end)))
                {
                    return false;
                }

                return IsValueValid(op, value) && 0 <= start && start < end;
            }
        }

        /// <summary>
        /// Creates an empty sheet.
        /// </summary>
        public BoostScheduleSheet() : base(nameof(BoostScheduleSheet))
        {
        }

        /// <summary>
        /// Finds the row that applies to <paramref name="targetId"/> of
        /// <paramref name="target"/> at <paramref name="blockIndex"/>.
        /// </summary>
        /// <param name="target">The target being adjusted. See <see cref="Targets"/>.</param>
        /// <param name="targetId">The id within the target.</param>
        /// <param name="blockIndex">The block being evaluated.</param>
        /// <returns>The matching row with the lowest id, or <c>null</c> when none applies.</returns>
        public Row FindActive(string target, int targetId, long blockIndex)
        {
            if (OrderedList is null)
            {
                return null;
            }

            foreach (var row in OrderedList)
            {
                if (row.Matches(target, targetId, blockIndex))
                {
                    return row;
                }
            }

            return null;
        }

        /// <summary>
        /// Adds a parsed row to the sheet.
        /// </summary>
        /// <param name="key">The row's key.</param>
        /// <param name="value">The row to add.</param>
        /// <remarks>
        /// A row without a usable id is dropped and a duplicated id keeps the first row instead of
        /// throwing, because a throw here would fail every action that reads the sheet.
        /// </remarks>
        protected override void AddRow(int key, Row value)
        {
            if (key <= 0 || ContainsKey(key))
            {
                return;
            }

            base.AddRow(key, value);
        }

        /// <summary>
        /// Parses <paramref name="csv"/> strictly so that a malformed schedule fails the
        /// <c>patch_table_sheet</c> transaction instead of landing on the chain as a row that
        /// silently never applies.
        /// </summary>
        /// <param name="csv">The sheet's CSV, as the patch would write it.</param>
        /// <exception cref="SheetRowValidateException">
        /// The header does not match <see cref="ColumnNames"/>, or a row has an unusable or
        /// duplicated id, an empty target, an unparsable cell, a value out of range for its
        /// operation, or a block range that is empty.
        /// </exception>
        /// <remarks>
        /// Whether a target is applied by any running code is deliberately not checked here: the
        /// set of targets grows with releases, and a patch must not be accepted by one node version
        /// and rejected by another. Compare against <see cref="Targets.Applied"/> off-chain.
        /// </remarks>
        public static void ValidateCsv(string csv)
        {
            var lines = csv.Trim().Split('\n');

            // Mirror Sheet<TKey, TValue>.Set: it drops "_" prefixed columns from every line
            // based on the header, so validating raw fields would judge different columns than
            // the ones the runtime reads.
            var droppedColumns = new HashSet<int>();
            var headerFields = lines[0].Trim().Split(',');
            for (var i = 0; i < headerFields.Length; i++)
            {
                if (headerFields[i].StartsWith("_"))
                {
                    droppedColumns.Add(i);
                }
            }

            var columnNames = Select(headerFields, droppedColumns);
            if (!columnNames.SequenceEqual(ColumnNames))
            {
                throw new SheetRowValidateException(
                    $"header must be \"{string.Join(",", ColumnNames)}\"" +
                    $" but was \"{string.Join(",", columnNames)}\".");
            }

            // Sheet.Set skips any line starting with "," or "_", so a dropped column in front
            // would make every row whose memo is empty (or starts with "_") vanish.
            if (droppedColumns.Contains(0))
            {
                throw new SheetRowValidateException(
                    $"the first column must be \"{ColumnNames[0]}\"; put \"_\" columns after it.");
            }

            var ids = new HashSet<int>();
            var lineNumber = 1;
            foreach (var rawLine in lines.Skip(1))
            {
                lineNumber++;

                // Sheet.Set skips these lines, so a row written there would never apply. Only a
                // blank line — what a spreadsheet export leaves as ",,,,,," — may be skipped.
                if (rawLine.Trim().Trim(',').Trim().Length == 0)
                {
                    continue;
                }

                if (rawLine.StartsWith(",") || rawLine.StartsWith("_"))
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: starts with \"{rawLine[0]}\" so the sheet would skip" +
                        " it. Is the id missing?");
                }

                var fields = Select(rawLine.Trim().Split(','), droppedColumns);
                if (fields.Count != ColumnNames.Length)
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: expected {ColumnNames.Length} cells" +
                        $" but found {fields.Count}. A \"_\" column cannot contain a comma.");
                }

                var row = new Row();
                row.Set(fields);
                if (row.Id <= 0)
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: id({fields[0].Trim()}) must be a positive integer.");
                }

                if (!ids.Add(row.Id))
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: duplicated id({row.Id}).");
                }

                if (!row.IsValid)
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: {Describe(fields)}");
                }
            }

            // Guards against this validator and Sheet.Set disagreeing about which rows exist.
            var sheet = new BoostScheduleSheet();
            sheet.Set(csv);
            if (sheet.Count != ids.Count)
            {
                throw new SheetRowValidateException(
                    $"validated {ids.Count} row(s) but the sheet parsed {sheet.Count}.");
            }
        }

        private static string Describe(IReadOnlyList<string> fields)
        {
            var target = fields[1].Trim();
            if (target.Length == 0)
            {
                return "target is empty.";
            }

            var targetId = fields[2].Trim();
            if (!TryParseTargetIds(targetId, out _, out _))
            {
                return $"target_id({targetId}) must be a positive integer, a range such as" +
                       $" \"1{TargetIdRangeSeparator}50\" whose ends are positive and in order," +
                       $" or \"{AnyTargetId}\".";
            }

            if (!TryParseOperation(fields[3].Trim(), out var op))
            {
                return $"op({fields[3].Trim()}) must be MUL or ADD.";
            }

            if (!TryParseDecimal(fields[4].Trim(), out var value) || !IsValueValid(op, value))
            {
                return op == Operation.Add
                    ? $"value({fields[4].Trim()}) must be an integer for ADD" +
                      $" and at most {MaxAbsoluteValue} in magnitude."
                    : $"value({fields[4].Trim()}) must be greater than 0 for MUL" +
                      $" and at most {MaxAbsoluteValue}.";
            }

            if (!TryParseLong(fields[5].Trim(), out var start) || start < 0)
            {
                return $"start_block({fields[5].Trim()}) must be a non-negative integer.";
            }

            if (!TryParseLong(fields[6].Trim(), out var end) || end <= start)
            {
                return $"end_block({fields[6].Trim()}) must be an integer greater than" +
                       $" start_block({start}).";
            }

            return "row is invalid.";
        }

        private static bool TryParseTargetIds(string cell, out int? begin, out int? end)
        {
            begin = null;
            end = null;
            if (cell is null)
            {
                return false;
            }

            if (cell == AnyTargetId)
            {
                return true;
            }

            var ends = cell.Split(TargetIdRangeSeparator);
            if (ends.Length > 2 ||
                !TryParsePositiveInt(ends[0], out var parsedBegin) ||
                !TryParsePositiveInt(ends[ends.Length - 1], out var parsedEnd) ||
                parsedBegin > parsedEnd)
            {
                return false;
            }

            begin = parsedBegin;
            end = parsedEnd;
            return true;
        }

        private static bool TryParsePositiveInt(string cell, out int value) =>
            TryParseInt(cell.Trim(), out value) && value > 0;

        private static bool TryParseOperation(string cell, out Operation op)
        {
            switch (cell)
            {
                case "MUL":
                    op = Operation.Mul;
                    return true;
                case "ADD":
                    op = Operation.Add;
                    return true;
                default:
                    op = default;
                    return false;
            }
        }

        private static bool IsValueValid(Operation op, decimal value) =>
            op == Operation.Add
                ? decimal.Truncate(value) == value && Math.Abs(value) <= MaxAbsoluteValue
                : value > 0m && value <= MaxAbsoluteValue;

        private static string Cell(IReadOnlyList<string> fields, int index)
        {
            if (fields.Count <= index)
            {
                return null;
            }

            var cell = fields[index].Trim();
            return cell.Length == 0 ? null : cell;
        }

        private static IReadOnlyList<string> Select(
            IReadOnlyList<string> fields,
            ICollection<int> droppedColumns) =>
            fields.Where((_, index) => !droppedColumns.Contains(index)).ToList();
    }
}
