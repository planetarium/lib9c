using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.TableData
{
    /// <summary>
    /// The conditions that open a <see cref="WorldOpenSheet"/> opening: within one
    /// <c>condition_group</c> every counter must reach its threshold, and any one group being
    /// satisfied opens it (AND within a group, OR across groups).
    /// <para>
    /// Columns: <c>open_id</c>, <c>condition_group</c>, <c>counter_id</c>, <c>threshold</c>.
    /// Cross-sheet references (<c>open_id</c>, <c>counter_id</c>) are not validated here.
    /// </para>
    /// <para>
    /// <c>open_id</c> is not unique, so a row's key is a synthetic sequential number assigned in
    /// the order rows are added (CSV order, starting at 1). Look rows up with
    /// <see cref="GroupsOf"/> or <see cref="OpenIdsReferencing"/> rather than by key. A blank line
    /// is kept as an invalid row (open_id 0); both lookups skip invalid rows, so code reading
    /// <c>OrderedList</c> directly must check <see cref="Row.IsValid"/> too.
    /// </para>
    /// <para>
    /// A patch cannot change what an id means once recorded; see
    /// <see cref="Nekoyume.Action.WorldOpenIdentity"/>.
    /// </para>
    /// </summary>
    [Serializable]
    public class WorldOpenConditionSheet : Sheet<int, WorldOpenConditionSheet.Row>
    {
        /// <summary>
        /// Column names in their expected order, after <c>_</c> prefixed columns are dropped.
        /// </summary>
        public static readonly string[] ColumnNames =
        {
            "open_id",
            "condition_group",
            "counter_id",
            "threshold",
        };

        private int _lastKey;

        /// <summary>
        /// One counter requirement of a condition group.
        /// </summary>
        [Serializable]
        public class Row : SheetRow<int>
        {
            /// <summary>
            /// The synthetic row number, assigned when the row is added to the sheet.
            /// </summary>
            public override int Key => RowKey;

            /// <summary>The opening this condition belongs to.</summary>
            public int OpenId { get; private set; }

            /// <summary>The group; counters of one group are AND-ed, groups are OR-ed.</summary>
            public int ConditionGroup { get; private set; }

            /// <summary>The <see cref="GlobalCounterSheet"/> counter that is measured.</summary>
            public int CounterId { get; private set; }

            /// <summary>The count the counter must reach.</summary>
            public long Threshold { get; private set; }

            /// <summary>Whether every cell parsed and is in range.</summary>
            public bool IsValid { get; private set; }

            private int RowKey { get; set; }

            internal void AssignKey(int key) => RowKey = key;

            /// <inheritdoc />
            public override void Set(IReadOnlyList<string> fields)
            {
                var ok = WorldOpenCsv.TryInt(fields, 0, out var openId);
                ok &= WorldOpenCsv.TryInt(fields, 1, out var group);
                ok &= WorldOpenCsv.TryInt(fields, 2, out var counterId);
                ok &= WorldOpenCsv.TryLong(fields, 3, out var threshold);
                OpenId = openId;
                ConditionGroup = group;
                CounterId = counterId;
                Threshold = threshold;
                IsValid = ok && openId > 0 && group > 0 && counterId > 0 && threshold > 0;
            }
        }

        /// <summary>
        /// Creates an empty sheet.
        /// </summary>
        public WorldOpenConditionSheet() : base(nameof(WorldOpenConditionSheet))
        {
        }

        /// <summary>
        /// The ids of the openings that have a condition on <paramref name="counterId"/>.
        /// </summary>
        public IEnumerable<int> OpenIdsReferencing(int counterId)
        {
            if (OrderedList is null)
            {
                return Enumerable.Empty<int>();
            }

            return OrderedList
                .Where(row => row.IsValid && row.CounterId == counterId)
                .Select(row => row.OpenId)
                .Distinct();
        }

        /// <summary>
        /// The condition groups of <paramref name="openId"/>, each as the rows (counters) of that
        /// group, ordered by group then by row order.
        /// </summary>
        public IEnumerable<IReadOnlyList<Row>> GroupsOf(int openId)
        {
            if (OrderedList is null)
            {
                return Enumerable.Empty<IReadOnlyList<Row>>();
            }

            return OrderedList
                .Where(row => row.IsValid && row.OpenId == openId)
                .GroupBy(row => row.ConditionGroup)
                .OrderBy(group => group.Key)
                .Select(group => (IReadOnlyList<Row>)group.ToList());
        }

        /// <summary>
        /// Assigns the synthetic key. Rows are numbered in the order they are added.
        /// </summary>
        protected override void AddRow(int key, Row value)
        {
            value.AssignKey(++_lastKey);
            base.AddRow(value.Key, value);
        }

        /// <summary>
        /// Parses <paramref name="csv"/> strictly so that a malformed sheet fails the
        /// <c>patch_table_sheet</c> transaction instead of landing on the chain.
        /// </summary>
        /// <exception cref="SheetRowValidateException">
        /// The header does not match <see cref="ColumnNames"/>, a cell does not parse, or
        /// <c>open_id</c>, <c>condition_group</c>, <c>counter_id</c> or <c>threshold</c> is not
        /// positive.
        /// </exception>
        /// <remarks>
        /// References to other sheets are not checked here; the backoffice does that.
        /// </remarks>
        public static void ValidateCsv(string csv)
        {
            var count = 0;
            foreach (var (lineNumber, fields) in WorldOpenCsv.ReadRows(csv, ColumnNames))
            {
                var row = new Row();
                row.Set(fields);
                if (!row.IsValid)
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: open_id, condition_group, counter_id and threshold " +
                        "must be positive integers.");
                }

                count++;
            }

            // Guards against this validator and Sheet.Set disagreeing about which rows exist. Only
            // valid rows count: Sheet.Set keeps a blank line as an invalid row (the synthetic key
            // never drops it), and lookups skip invalid rows.
            var sheet = new WorldOpenConditionSheet();
            sheet.Set(csv);
            var parsed = sheet.OrderedList?.Count(row => row.IsValid) ?? 0;
            if (parsed != count)
            {
                throw new SheetRowValidateException(
                    $"validated {count} row(s) but the sheet parsed {parsed}.");
            }
        }
    }
}
