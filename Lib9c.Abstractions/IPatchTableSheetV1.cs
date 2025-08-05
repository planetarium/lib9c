namespace Lib9c.Abstractions
{
    public interface IPatchTableSheetV1
    {
        string TableName { get; }
        string TableCsv { get; }
    }

    /// <summary>
    /// Interface for patching table sheets with compressed CSV data to reduce transaction size.
    /// </summary>
    public interface IPatchTableSheetCompressedV1
    {
        string TableName { get; }
        byte[] CompressedTableCsv { get; }
    }
}
