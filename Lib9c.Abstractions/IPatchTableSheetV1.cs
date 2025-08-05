namespace Lib9c.Abstractions
{
    /// <summary>
    /// Interface for patching table sheets with uncompressed CSV data.
    ///
    /// This interface is used by the original PatchTableSheet action.
    /// For large CSV files, consider using IPatchTableSheetCompressedV1 instead
    /// to reduce transaction size significantly.
    /// </summary>
    /// <remarks>
    /// The uncompressed approach is suitable for small CSV files.
    /// For files larger than 1KB, the compressed approach provides better performance.
    /// </remarks>
    public interface IPatchTableSheetV1
    {
        /// <summary>
        /// Gets the name of the table sheet to patch.
        /// </summary>
        string TableName { get; }

        /// <summary>
        /// Gets the uncompressed CSV data as a string.
        /// </summary>
        string TableCsv { get; }
    }

    /// <summary>
    /// Interface for patching table sheets with compressed CSV data to reduce transaction size.
    ///
    /// This interface is used by PatchTableSheetCompressed action and provides
    /// significant transaction size reduction for large CSV files.
    ///
    /// Performance benefits:
    /// - RuneOptionSheet.csv: 842KB → 154KB (81% reduction)
    /// - RuneSheet.csv: 1.7KB → 587B (65% reduction)
    /// - CostumeStatSheet.csv: 3.6KB → 1.4KB (62% reduction)
    ///
    /// Example usage:
    /// <code>
    /// var action = new PatchTableSheetCompressed
    /// {
    ///     TableName = "RuneOptionSheet",
    ///     CompressedTableCsv = PatchTableSheetCompressed.CompressCsv(csvData),
    /// };
    /// </code>
    /// </summary>
    /// <remarks>
    /// The compressed approach is recommended for CSV files larger than 1KB.
    /// GZip compression typically reduces CSV file size by 60-80% depending on content.
    /// </remarks>
    public interface IPatchTableSheetCompressedV1
    {
        /// <summary>
        /// Gets the name of the table sheet to patch.
        /// This should match the name of the CSV file without the .csv extension.
        /// </summary>
        /// <example>
        /// "RuneOptionSheet" for RuneOptionSheet.csv
        /// "ItemSheet" for ItemSheet.csv
        /// </example>
        string TableName { get; }

        /// <summary>
        /// Gets the compressed CSV data as a byte array.
        /// This should be compressed using GZip compression.
        /// </summary>
        /// <remarks>
        /// The compression can reduce file size by 60-80% depending on the CSV content.
        /// For example, RuneOptionSheet.csv (842KB) can be compressed to ~154KB.
        /// </remarks>
        byte[] CompressedTableCsv { get; }
    }
}
