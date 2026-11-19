using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.TableData
{
    /// <summary>
    /// Shared strict CSV reading for the <c>ValidateCsv</c> of the world-open sheets
    /// (<see cref="GlobalCounterSheet"/>, <see cref="WorldOpenSheet"/>,
    /// <see cref="WorldOpenConditionSheet"/>). Mirrors how <c>Sheet.Set</c> reads a CSV, so the
    /// validator judges the same columns and lines the runtime reads.
    /// </summary>
    internal static class WorldOpenCsv
    {
        /// <summary>
        /// Checks the header and returns every data line's cells, with <c>_</c> prefixed columns
        /// dropped and blank lines skipped, together with the 1-based line number.
        /// </summary>
        /// <exception cref="SheetRowValidateException">
        /// The header does not match <paramref name="columnNames"/>, a line the sheet would skip
        /// silently carries data, or a line has the wrong number of cells.
        /// </exception>
        internal static List<(int LineNumber, IReadOnlyList<string> Fields)> ReadRows(
            string csv,
            string[] columnNames)
        {
            var lines = csv.Trim().Split('\n');
            var droppedColumns = new HashSet<int>();
            var headerFields = lines[0].Trim().Split(',');
            for (var i = 0; i < headerFields.Length; i++)
            {
                if (headerFields[i].StartsWith("_"))
                {
                    droppedColumns.Add(i);
                }
            }

            var header = Select(headerFields, droppedColumns);
            if (!header.SequenceEqual(columnNames))
            {
                throw new SheetRowValidateException(
                    $"header must be \"{string.Join(",", columnNames)}\"" +
                    $" but was \"{string.Join(",", header)}\".");
            }

            if (droppedColumns.Contains(0))
            {
                throw new SheetRowValidateException(
                    $"the first column must be \"{columnNames[0]}\"; put \"_\" columns after it.");
            }

            var rows = new List<(int, IReadOnlyList<string>)>();
            var lineNumber = 1;
            foreach (var rawLine in lines.Skip(1))
            {
                lineNumber++;
                if (rawLine.Trim().Trim(',').Trim().Length == 0)
                {
                    continue;
                }

                if (rawLine.StartsWith(",") || rawLine.StartsWith("_"))
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: starts with \"{rawLine[0]}\" so the sheet would skip" +
                        $" it. Is the {columnNames[0]} missing?");
                }

                var fields = Select(rawLine.Trim().Split(','), droppedColumns);
                if (fields.Count != columnNames.Length)
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: expected {columnNames.Length} cells" +
                        $" but found {fields.Count}. A \"_\" column cannot contain a comma.");
                }

                rows.Add((lineNumber, fields));
            }

            return rows;
        }

        /// <summary>
        /// The cell at <paramref name="index"/>, trimmed, or <c>null</c> when absent or empty.
        /// </summary>
        internal static string Cell(IReadOnlyList<string> fields, int index)
        {
            if (fields.Count <= index)
            {
                return null;
            }

            var cell = fields[index].Trim();
            return cell.Length == 0 ? null : cell;
        }

        internal static bool TryInt(IReadOnlyList<string> fields, int index, out int value)
        {
            value = default;
            return Cell(fields, index) is { } cell && TableExtensions.TryParseInt(cell, out value);
        }

        internal static bool TryLong(IReadOnlyList<string> fields, int index, out long value)
        {
            value = default;
            return Cell(fields, index) is { } cell && TableExtensions.TryParseLong(cell, out value);
        }

        private static List<string> Select(
            IReadOnlyList<string> fields,
            ICollection<int> droppedColumns) =>
            fields.Where((_, index) => !droppedColumns.Contains(index)).ToList();
    }
}
