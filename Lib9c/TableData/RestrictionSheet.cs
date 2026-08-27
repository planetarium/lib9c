using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    /// <summary>
    /// Declares what a given item or currency may not be used for, so that a restriction can be
    /// adjusted with <c>patch_table_sheet</c> instead of a headless and client release.
    /// <para>
    /// A row targets either an item id (numeric key) or a currency ticker (non-numeric key).
    /// Which one is looked up is decided by the call site — a market action already holds an item
    /// id or a ticker — so the sheet needs no discriminator column.
    /// </para>
    /// <para>
    /// Columns: <c>key</c>, <c>market_registrable</c>, <c>synthesize_material</c>.
    /// The first governs the market registration path only — a fungible item can still reach
    /// another account through a garage or by being wrapped into a currency, so a row here does
    /// not make its target account bound by itself. The second closes the conversion path that
    /// would otherwise turn a restricted item into an unrestricted one.
    /// </para>
    /// <para>
    /// An empty cell means "unspecified" and leaves the behavior that predates this sheet
    /// untouched. An unparsable cell is read as <c>false</c>(= restricted) on purpose: a typo then
    /// fails toward blocking, which is recoverable, rather than toward leaking.
    /// <see cref="ValidateCsv"/> rejects such a cell at patch time so it should not reach the
    /// chain in the first place.
    /// </para>
    /// <para>
    /// Callers must tolerate the sheet being absent — it reaches an existing chain only by
    /// <c>patch_table_sheet</c>, so loading it as a required sheet would fail every affected
    /// action until each chain is patched. Absence has to fall back to the restrictions that are
    /// still hardcoded, which is also what keeps re-evaluation of pre-patch blocks identical.
    /// </para>
    /// </summary>
    [Serializable]
    public class RestrictionSheet : Sheet<string, RestrictionSheet.Row>
    {
        /// <summary>
        /// Column names in their expected order, after <c>_</c> prefixed columns are dropped.
        /// </summary>
        public static readonly string[] ColumnNames =
        {
            "key",
            "market_registrable",
            "synthesize_material",
        };

        /// <summary>
        /// One target and what it may not be used for.
        /// </summary>
        [Serializable]
        public class Row : SheetRow<string>
        {
            /// <summary>
            /// The item id or ticker this row restricts, which is also how it is looked up.
            /// </summary>
            public override string Key => Target;

            /// <summary>
            /// An item id in its string form, or a currency ticker.
            /// </summary>
            public string Target { get; private set; } = string.Empty;

            /// <summary>
            /// Whether the target may be registered on the market.
            /// <c>null</c> when the cell is empty(= unspecified).
            /// </summary>
            public bool? MarketRegistrable { get; private set; }

            /// <summary>
            /// Whether the target may be consumed as a synthesis material.
            /// <c>null</c> when the cell is empty(= unspecified).
            /// </summary>
            public bool? SynthesizeMaterial { get; private set; }

            /// <summary>
            /// Reads one row.
            /// </summary>
            /// <param name="fields">The row's cells, with "_" prefixed columns already dropped.</param>
            /// <remarks>
            /// Total by design: <see cref="Sheet{TKey,TValue}"/> does not guard this call, so a
            /// throw here would abort the whole sheet and take every restriction with it.
            /// </remarks>
            public override void Set(IReadOnlyList<string> fields)
            {
                Target = fields.Count > 0 ? fields[0].Trim() : string.Empty;
                MarketRegistrable = ParsePolicy(fields, 1);
                SynthesizeMaterial = ParsePolicy(fields, 2);
            }

            internal void MergeRestrictive(Row other)
            {
                MarketRegistrable = PickRestrictive(MarketRegistrable, other.MarketRegistrable);
                SynthesizeMaterial = PickRestrictive(SynthesizeMaterial, other.SynthesizeMaterial);
            }

            private static bool? ParsePolicy(IReadOnlyList<string> fields, int index)
            {
                if (fields.Count <= index)
                {
                    return null;
                }

                var raw = fields[index].Trim();
                if (raw.Length == 0)
                {
                    return null;
                }

                // An unparsable cell restricts. ValidateCsv rejects it at patch time.
                return ParseBool(raw, false);
            }

            private static bool? PickRestrictive(bool? left, bool? right)
            {
                if (left is null)
                {
                    return right;
                }

                if (right is null)
                {
                    return left;
                }

                return left.Value && right.Value;
            }
        }

        /// <summary>
        /// Creates an empty sheet.
        /// </summary>
        public RestrictionSheet() : base(nameof(RestrictionSheet))
        {
        }

        /// <summary>
        /// Renders an item id the way a row key spells it.
        /// </summary>
        public static string ToKey(int itemId) => itemId.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Whether the item may be registered on the market. True when unlisted or unspecified.
        /// </summary>
        public bool IsItemMarketRegistrable(int itemId) =>
            !TryGetValue(ToKey(itemId), out var row) || row.MarketRegistrable is not false;

        /// <summary>
        /// Whether the currency may be registered on the market as a fungible asset product.
        /// True when unlisted or unspecified.
        /// </summary>
        public bool IsCurrencyMarketRegistrable(string ticker) =>
            !TryGetValue(ticker, out var row) || row.MarketRegistrable is not false;

        /// <summary>
        /// Whether the item may be consumed as a synthesis material.
        /// True when unlisted or unspecified.
        /// </summary>
        public bool IsItemSynthesizeMaterial(int itemId) =>
            !TryGetValue(ToKey(itemId), out var row) || row.SynthesizeMaterial is not false;

        /// <summary>
        /// Adds a parsed row to the sheet.
        /// </summary>
        /// <param name="key">The row's key.</param>
        /// <param name="value">The row to add.</param>
        /// <remarks>
        /// Blank keys are dropped and duplicate keys are merged toward the restrictive value
        /// instead of throwing, because a throw here would take the entire policy offline and
        /// with it every restriction the sheet carries.
        /// </remarks>
        protected override void AddRow(string key, Row value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (TryGetValue(key, out var existing))
            {
                existing.MergeRestrictive(value);
                return;
            }

            base.AddRow(key, value);
        }

        /// <summary>
        /// Parses <paramref name="csv"/> strictly so that a malformed policy fails the
        /// <c>patch_table_sheet</c> transaction instead of silently landing on the chain, where
        /// a row that no lookup can match reads as "unrestricted".
        /// </summary>
        /// <param name="csv">The sheet's CSV, as the patch would write it.</param>
        /// <exception cref="SheetRowValidateException">
        /// The header does not match <see cref="ColumnNames"/>, or a row has no key, a key no
        /// lookup can produce, an unparsable policy cell, no policy at all, a synthesis policy
        /// on a currency, or a key an earlier row already claimed.
        /// </exception>
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

            var keys = new HashSet<string>();
            var lineNumber = 1;
            foreach (var rawLine in lines.Skip(1))
            {
                lineNumber++;

                // Sheet.Set decides to skip before trimming, so this has to as well.
                if (rawLine.StartsWith(",") ||
                    rawLine.StartsWith("_") ||
                    rawLine.Trim().Length == 0)
                {
                    continue;
                }

                var fields = Select(rawLine.Trim().Split(','), droppedColumns);
                var key = fields[0].Trim();
                if (key.Length == 0)
                {
                    throw new SheetRowValidateException($"line {lineNumber}: key is empty.");
                }

                var isItemId = IsItemIdKey(key);
                if (!isItemId && !IsTickerKey(key))
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: key({key}) is neither an item id nor a ticker." +
                        " An item id must be written the way int.ToString() renders it and a" +
                        " ticker must be ASCII, starting with an upper case letter.");
                }

                if (!keys.Add(key))
                {
                    throw new SheetRowValidateException($"line {lineNumber}: duplicated key({key}).");
                }

                var specified = false;
                for (var i = 1; i < ColumnNames.Length; i++)
                {
                    if (fields.Count <= i)
                    {
                        continue;
                    }

                    var cell = fields[i].Trim();
                    if (cell.Length == 0)
                    {
                        continue;
                    }

                    if (!bool.TryParse(cell, out _))
                    {
                        throw new SheetRowValidateException(
                            $"line {lineNumber}: {ColumnNames[i]}({cell}) is not a boolean.");
                    }

                    if (i == 2 && !isItemId)
                    {
                        throw new SheetRowValidateException(
                            $"line {lineNumber}: {ColumnNames[i]} does not apply to a currency" +
                            $" key({key}); synthesis consumes items only.");
                    }

                    specified = true;
                }

                if (!specified)
                {
                    throw new SheetRowValidateException(
                        $"line {lineNumber}: key({key}) specifies no policy.");
                }
            }

            // Guards against this validator and Sheet.Set disagreeing about which rows exist.
            var sheet = new RestrictionSheet();
            sheet.Set(csv);
            if (sheet.Count != keys.Count)
            {
                throw new SheetRowValidateException(
                    $"validated {keys.Count} row(s) but the sheet parsed {sheet.Count}.");
            }
        }

        private static IReadOnlyList<string> Select(
            IReadOnlyList<string> fields,
            ICollection<int> droppedColumns) =>
            fields.Where((_, index) => !droppedColumns.Contains(index)).ToList();

        private static bool IsItemIdKey(string key) =>
            int.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out var id) &&
            id > 0 &&
            key == ToKey(id);

        private static bool IsTickerKey(string key) =>
            key[0] >= 'A' && key[0] <= 'Z' &&
            key.All(c =>
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                (c >= '0' && c <= '9') ||
                c == '_');
    }
}
