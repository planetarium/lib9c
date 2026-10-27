using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.TableData
{
    /// <summary>
    /// What a <see cref="GlobalCounterSheet"/> counter counts.
    /// </summary>
    public enum GlobalCounterType
    {
        /// <summary>An avatar clears a stage for the first time.</summary>
        STAGE_FIRST_CLEAR,

        /// <summary>
        /// An avatar wins a <c>HackAndSlash</c> battle on a stage, counted per battle won (a
        /// multi-play action adds its wins; a lost battle adds nothing). Sweeping does not count.
        /// </summary>
        STAGE_PLAY,

        /// <summary>An avatar crafts a recipe.</summary>
        RECIPE_CRAFT,
    }

    /// <summary>
    /// Defines the global counters that world-open conditions
    /// (<see cref="WorldOpenConditionSheet"/>) are measured with: what each counter counts, over
    /// which targets, and the minimum avatar level that contributes.
    /// <para>
    /// Columns: <c>id</c>, <c>type</c>, <c>target_begin</c>, <c>target_end</c>,
    /// <c>min_avatar_level</c>. The target is a stage id or a recipe id depending on
    /// <c>type</c>; <c>[target_begin, target_end]</c> is inclusive.
    /// </para>
    /// <para>
    /// Reading is total, like <see cref="BoostScheduleSheet"/>: a row that does not parse is kept
    /// but marked invalid and never matches. <see cref="ValidateCsv"/> rejects such a row at
    /// patch time.
    /// </para>
    /// </summary>
    [Serializable]
    public class GlobalCounterSheet : Sheet<int, GlobalCounterSheet.Row>
    {
        /// <summary>
        /// Column names in their expected order, after <c>_</c> prefixed columns are dropped.
        /// </summary>
        public static readonly string[] ColumnNames =
        {
            "id",
            "type",
            "target_begin",
            "target_end",
            "min_avatar_level",
        };

        /// <summary>
        /// One counter definition.
        /// </summary>
        [Serializable]
        public class Row : SheetRow<int>
        {
            /// <inheritdoc />
            public override int Key => Id;

            /// <summary>The counter id.</summary>
            public int Id { get; private set; }

            /// <summary>What the counter counts.</summary>
            public GlobalCounterType Type { get; private set; }

            /// <summary>First target id counted, inclusive.</summary>
            public int TargetBegin { get; private set; }

            /// <summary>Last target id counted, inclusive.</summary>
            public int TargetEnd { get; private set; }

            /// <summary>Minimum avatar level whose action counts.</summary>
            public int MinAvatarLevel { get; private set; }

            /// <summary>Whether every cell parsed. An invalid row never matches.</summary>
            public bool IsValid { get; private set; }

            /// <inheritdoc />
            public override void Set(IReadOnlyList<string> fields)
            {
                var ok = WorldOpenCsv.TryInt(fields, 0, out var id);
                Id = ok ? id : 0;

                var typeCell = WorldOpenCsv.Cell(fields, 1);
                var typeOk = typeCell is not null &&
                             Enum.GetNames(typeof(GlobalCounterType)).Contains(typeCell);
                Type = typeOk ? (GlobalCounterType)Enum.Parse(typeof(GlobalCounterType), typeCell) : default;

                ok &= typeOk;
                ok &= WorldOpenCsv.TryInt(fields, 2, out var begin);
                ok &= WorldOpenCsv.TryInt(fields, 3, out var end);
                ok &= WorldOpenCsv.TryInt(fields, 4, out var minLevel);
                TargetBegin = begin;
                TargetEnd = end;
                MinAvatarLevel = minLevel;
                IsValid = ok && Id > 0;
            }
        }

        /// <summary>
        /// Creates an empty sheet.
        /// </summary>
        public GlobalCounterSheet() : base(nameof(GlobalCounterSheet))
        {
        }

        /// <summary>
        /// The valid counters of <paramref name="type"/> whose target range contains
        /// <paramref name="target"/>.
        /// </summary>
        public IEnumerable<Row> Matching(GlobalCounterType type, int target)
        {
            if (OrderedList is null)
            {
                return Enumerable.Empty<Row>();
            }

            return OrderedList.Where(row =>
                row.IsValid &&
                row.Type == type &&
                row.TargetBegin <= target &&
                target <= row.TargetEnd);
        }

        /// <summary>
        /// Adds a parsed row. A row without a usable id is dropped and a duplicated id keeps the
        /// first row instead of throwing, because a throw would fail every action that reads the
        /// sheet.
        /// </summary>
        protected override void AddRow(int key, Row value)
        {
            if (key <= 0 || ContainsKey(key))
            {
                return;
            }

            base.AddRow(key, value);
        }

        /// <summary>
        /// Parses <paramref name="csv"/> strictly so that a malformed sheet fails the
        /// <c>patch_table_sheet</c> transaction instead of landing on the chain.
        /// </summary>
        /// <exception cref="SheetRowValidateException">
        /// The header does not match <see cref="ColumnNames"/>, an id is not a positive unique
        /// integer, a cell does not parse, the type is unknown, <c>target_begin</c> is greater
        /// than <c>target_end</c>, or a <c>STAGE_FIRST_CLEAR</c> counter spans more than one
        /// stage.
        /// </exception>
        public static void ValidateCsv(string csv)
        {
            var ids = new HashSet<int>();
            foreach (var (lineNumber, fields) in WorldOpenCsv.ReadRows(csv, ColumnNames))
            {
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
                        $"line {lineNumber}: type must be one of " +
                        $"{string.Join("/", Enum.GetNames(typeof(GlobalCounterType)))} and " +
                        "the other cells integers.");
                }

                if (row.TargetBegin > row.TargetEnd)
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: target_begin({row.TargetBegin}) must not be greater " +
                        $"than target_end({row.TargetEnd}).");
                }

                if (row.Type == GlobalCounterType.STAGE_FIRST_CLEAR &&
                    row.TargetBegin != row.TargetEnd)
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: {row.Type} counts a single stage, so " +
                        $"target_begin({row.TargetBegin}) must equal target_end({row.TargetEnd}).");
                }
            }

            // Guards against this validator and Sheet.Set disagreeing about which rows exist.
            var sheet = new GlobalCounterSheet();
            sheet.Set(csv);
            if (sheet.Count != ids.Count)
            {
                throw new SheetRowValidateException(
                    $"validated {ids.Count} row(s) but the sheet parsed {sheet.Count}.");
            }
        }
    }
}
