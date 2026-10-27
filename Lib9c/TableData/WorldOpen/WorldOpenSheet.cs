using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.TableData
{
    /// <summary>
    /// Defines when each hard world segment opens: up to which stage, which opening it follows,
    /// and the block window (earliest, fallback, activation delay) around it. The conditions that
    /// open it are in <see cref="WorldOpenConditionSheet"/>.
    /// <para>
    /// Columns: <c>id</c>, <c>world_id</c>, <c>stage_end</c>, <c>prerequisite_open_id</c>
    /// (empty for none), <c>min_block_index</c>, <c>fallback_block_index</c>,
    /// <c>effective_delay_block</c>.
    /// </para>
    /// <para>
    /// <see cref="Row.Set"/> does not validate across rows, so a test can build an invalid sheet
    /// directly; <see cref="ValidateCsv"/> does, at patch time.
    /// </para>
    /// </summary>
    [Serializable]
    public class WorldOpenSheet : Sheet<int, WorldOpenSheet.Row>
    {
        /// <summary>
        /// Column names in their expected order, after <c>_</c> prefixed columns are dropped.
        /// </summary>
        public static readonly string[] ColumnNames =
        {
            "id",
            "world_id",
            "stage_end",
            "prerequisite_open_id",
            "min_block_index",
            "fallback_block_index",
            "effective_delay_block",
        };

        /// <summary>
        /// One opening.
        /// </summary>
        [Serializable]
        public class Row : SheetRow<int>
        {
            /// <inheritdoc />
            public override int Key => Id;

            /// <summary>The opening id.</summary>
            public int Id { get; private set; }

            /// <summary>The world this opening belongs to.</summary>
            public int WorldId { get; private set; }

            /// <summary>The last stage this opening makes playable.</summary>
            public int StageEnd { get; private set; }

            /// <summary>The opening that must happen first, or <c>null</c>.</summary>
            public int? PrerequisiteOpenId { get; private set; }

            /// <summary>Earliest block the opening may happen at.</summary>
            public long MinBlockIndex { get; private set; }

            /// <summary>Block at which the opening happens regardless of conditions.</summary>
            public long FallbackBlockIndex { get; private set; }

            /// <summary>Blocks between the opening being decided and taking effect.</summary>
            public long EffectiveDelayBlock { get; private set; }

            /// <summary>Whether every cell parsed.</summary>
            public bool IsValid { get; private set; }

            /// <inheritdoc />
            public override void Set(IReadOnlyList<string> fields)
            {
                var ok = WorldOpenCsv.TryInt(fields, 0, out var id);
                Id = ok ? id : 0;
                ok &= WorldOpenCsv.TryInt(fields, 1, out var worldId);
                ok &= WorldOpenCsv.TryInt(fields, 2, out var stageEnd);

                int? prerequisite = null;
                if (WorldOpenCsv.Cell(fields, 3) is not null)
                {
                    if (WorldOpenCsv.TryInt(fields, 3, out var prerequisiteId))
                    {
                        prerequisite = prerequisiteId;
                    }
                    else
                    {
                        ok = false;
                    }
                }

                ok &= WorldOpenCsv.TryLong(fields, 4, out var min);
                ok &= WorldOpenCsv.TryLong(fields, 5, out var fallback);
                ok &= WorldOpenCsv.TryLong(fields, 6, out var delay);
                WorldId = worldId;
                StageEnd = stageEnd;
                PrerequisiteOpenId = prerequisite;
                MinBlockIndex = min;
                FallbackBlockIndex = fallback;
                EffectiveDelayBlock = delay;
                IsValid = ok && Id > 0;
            }
        }

        /// <summary>
        /// Creates an empty sheet.
        /// </summary>
        public WorldOpenSheet() : base(nameof(WorldOpenSheet))
        {
        }

        /// <summary>
        /// The openings of <paramref name="worldId"/>.
        /// </summary>
        public IEnumerable<Row> ByWorld(int worldId)
        {
            if (OrderedList is null)
            {
                return Enumerable.Empty<Row>();
            }

            return OrderedList.Where(row => row.WorldId == worldId);
        }

        /// <summary>
        /// Adds a parsed row. A row without a usable id is dropped and a duplicated id keeps the
        /// first row instead of throwing.
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
        /// The header does not match <see cref="ColumnNames"/>; an id is not a positive unique
        /// integer; a cell does not parse; <c>fallback_block_index</c> is not positive;
        /// <c>min_block_index</c> is negative or greater than the fallback;
        /// <c>effective_delay_block</c> is negative or greater than the fallback; a prerequisite is unknown, refers to itself
        /// or is part of a cycle; <c>stage_end</c> does not increase along a prerequisite; or a
        /// row other than the one with the lowest <c>stage_end</c> does not take the row just
        /// below it (by <c>stage_end</c>, across all worlds) as its prerequisite.
        /// </exception>
        /// <remarks>
        /// References to other sheets (conditions, counters) are not checked here.
        /// </remarks>
        public static void ValidateCsv(string csv)
        {
            var rows = new Dictionary<int, Row>();
            foreach (var (lineNumber, fields) in WorldOpenCsv.ReadRows(csv, ColumnNames))
            {
                var row = new Row();
                row.Set(fields);
                if (row.Id <= 0)
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: id({fields[0].Trim()}) must be a positive integer.");
                }

                if (rows.ContainsKey(row.Id))
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: duplicated id({row.Id}).");
                }

                if (!row.IsValid)
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: every cell must be an integer " +
                        "(prerequisite_open_id may be empty).");
                }

                if (row.FallbackBlockIndex <= 0)
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: fallback_block_index({row.FallbackBlockIndex}) " +
                        "must be greater than 0.");
                }

                if (row.MinBlockIndex < 0 || row.MinBlockIndex > row.FallbackBlockIndex)
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: min_block_index({row.MinBlockIndex}) must be " +
                        $"between 0 and fallback_block_index({row.FallbackBlockIndex}).");
                }

                // A delay past the fallback can never take effect: the fallback always wins.
                if (row.EffectiveDelayBlock < 0 || row.EffectiveDelayBlock > row.FallbackBlockIndex)
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: effective_delay_block({row.EffectiveDelayBlock}) " +
                        $"must be between 0 and fallback_block_index({row.FallbackBlockIndex}).");
                }

                rows.Add(row.Id, row);
            }

            foreach (var row in rows.Values)
            {
                if (row.PrerequisiteOpenId is not { } prerequisiteId)
                {
                    continue;
                }

                if (!rows.TryGetValue(prerequisiteId, out var prerequisite))
                {
                    throw new SheetRowValidateException(
                        $"id({row.Id}): prerequisite_open_id({prerequisiteId}) does not exist.");
                }

                if (prerequisite.StageEnd >= row.StageEnd)
                {
                    throw new SheetRowValidateException(
                        $"id({row.Id}): stage_end({row.StageEnd}) must be greater than " +
                        $"stage_end({prerequisite.StageEnd}) of its prerequisite({prerequisiteId}).");
                }

                // Follow the prerequisite chain; a repeated id is a cycle (or self reference).
                var visited = new HashSet<int> { row.Id };
                var current = row;
                while (current.PrerequisiteOpenId is { } next)
                {
                    if (!visited.Add(next))
                    {
                        throw new SheetRowValidateException(
                            $"id({row.Id}): prerequisite chain loops back to id({next}).");
                    }

                    if (!rows.TryGetValue(next, out current))
                    {
                        throw new SheetRowValidateException(
                            $"id({row.Id}): prerequisite_open_id({next}) in its chain does not exist.");
                    }
                }
            }

            // The gate opens a world up to its largest applied stage_end, so a row must not be
            // able to apply before the rows below it. Stage ids run on across worlds, so all rows
            // form one line by stage_end: each row after the lowest takes the row just below it
            // as its prerequisite. "Some prerequisite" is not enough — one skipping a row would let
            // a fallback open everything up to it without that row's conditions.
            Row below = null;
            foreach (var row in rows.Values.OrderBy(r => r.StageEnd).ThenBy(r => r.Id))
            {
                if (below is not null && row.PrerequisiteOpenId != below.Id)
                {
                    throw new SheetRowValidateException(
                        $"id({row.Id}): id({below.Id}) opens up to stage {below.StageEnd} just " +
                        $"below this row, so prerequisite_open_id must be {below.Id}.");
                }

                below = row;
            }

            // Guards against this validator and Sheet.Set disagreeing about which rows exist.
            var sheet = new WorldOpenSheet();
            sheet.Set(csv);
            if (sheet.Count != rows.Count)
            {
                throw new SheetRowValidateException(
                    $"validated {rows.Count} row(s) but the sheet parsed {sheet.Count}.");
            }
        }
    }
}
