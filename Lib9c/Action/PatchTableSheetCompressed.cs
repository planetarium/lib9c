using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    /// <summary>
    /// Compressed version of PatchTableSheet to reduce transaction size for large CSV files.
    /// Uses GZip compression to minimize the size of table data in transactions.
    ///
    /// This action is particularly useful for large CSV files like RuneOptionSheet.csv
    /// which can be compressed from ~842KB to ~154KB (81% reduction).
    ///
    /// Example usage:
    /// <code>
    /// var compressedAction = new PatchTableSheetCompressed
    /// {
    ///     TableName = "RuneOptionSheet",
    ///     CompressedTableCsv = PatchTableSheetCompressed.CompressCsv(csvData),
    /// };
    /// </code>
    /// </summary>
    [Serializable]
    [ActionType("patch_table_sheet_compressed")]
    public class PatchTableSheetCompressed : GameAction, IPatchTableSheetCompressedV1
    {
        // FIXME: We should eliminate or justify this concept in another way after v100340.
        // (Until that) please consult Nine Chronicles Dev if you have any questions about this account.
        /// <summary>
        /// The operator address that has special permission to operation actions.
        /// When the action is signed by this operator, permission check is skipped.
        /// This is a temporary solution until v100340, after which this concept should be eliminated or justified differently.
        /// For any questions about this account, please consult Nine Chronicles Dev team.
        /// </summary>
        public static readonly Address Operator =
            new Address("3fe3106a3547488e157AED606587580e80375295");

        /// <summary>
        /// Gets or sets the name of the table sheet to patch.
        /// This should match the name of the CSV file without the .csv extension.
        /// </summary>
        /// <example>
        /// "RuneOptionSheet" for RuneOptionSheet.csv
        /// "ItemSheet" for ItemSheet.csv
        /// </example>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the compressed CSV data as a byte array.
        /// This should be compressed using the CompressCsv method.
        /// </summary>
        /// <remarks>
        /// The compression can reduce file size by 60-80% depending on the CSV content.
        /// For example, RuneOptionSheet.csv (842KB) can be compressed to ~154KB.
        /// </remarks>
        public byte[] CompressedTableCsv { get; set; }

        /// <summary>
        /// Explicit interface implementation for IPatchTableSheetCompressedV1.TableName.
        /// </summary>
        string IPatchTableSheetCompressedV1.TableName => TableName;

        /// <summary>
        /// Explicit interface implementation for IPatchTableSheetCompressedV1.CompressedTableCsv.
        /// </summary>
        byte[] IPatchTableSheetCompressedV1.CompressedTableCsv => CompressedTableCsv;

        /// <summary>
        /// Executes the compressed table sheet patching action.
        ///
        /// This method:
        /// 1. Validates permissions (unless signed by operator)
        /// 2. Decompresses the CSV data
        /// 3. Updates the table sheet state
        /// 4. Handles special cases like GameConfigSheet
        ///
        /// The action reduces transaction size significantly compared to PatchTableSheet
        /// while maintaining the same functionality.
        /// </summary>
        /// <param name="context">The action context containing state and transaction information.</param>
        /// <returns>The updated world state with the patched table sheet.</returns>
        /// <exception cref="InvalidDataException">Thrown when the compressed data is corrupted or invalid.</exception>
        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            IActionContext ctx = context;
            var states = ctx.PreviousState;
            var sheetAddress = Addresses.TableSheet.Derive(TableName);
            var addressesHex = GetSignerAndOtherAddressesHex(context);

#if !LIB9C_DEV_EXTENSIONS && !UNITY_EDITOR
            if (ctx.Signer == Operator)
            {
                Log.Information(
                    "Skip CheckPermission since {TxId} had been signed by the operator({Operator}).",
                    context.TxId,
                    Operator
                );
            }
            else
            {
                CheckPermission(context);
            }
#endif

            // Decompress CSV data
            var tableCsv = DecompressCsv(CompressedTableCsv);

            var sheet = states.GetLegacyState(sheetAddress);
            var value = sheet is null ? string.Empty : sheet.ToDotnetString();

            Log.Verbose(
                "{AddressesHex}{TableName} was patched (compressed)\n" +
                "before:\n" +
                "{Value}\n" +
                "after:\n" +
                "{TableCsv}",
                addressesHex,
                TableName,
                value,
                tableCsv
            );

            states = states.SetLegacyState(sheetAddress, tableCsv.Serialize());

            if (TableName == nameof(GameConfigSheet))
            {
                var gameConfigState = new GameConfigState(tableCsv);
                states = states.SetLegacyState(GameConfigState.Address, gameConfigState.Serialize());
            }

            return states;
        }

        /// <summary>
        /// Compresses CSV data using GZip compression.
        ///
        /// This method is used to prepare CSV data for the CompressedTableCsv property.
        /// GZip compression typically reduces CSV file size by 60-80%.
        ///
        /// Example:
        /// <code>
        /// var csvData = File.ReadAllText("RuneOptionSheet.csv");
        /// var compressedData = PatchTableSheetCompressed.CompressCsv(csvData);
        /// // compressedData can now be used in CompressedTableCsv property
        /// </code>
        /// </summary>
        /// <param name="csvData">The CSV data to compress. Must not be null.</param>
        /// <returns>Compressed byte array using GZip compression.</returns>
        /// <exception cref="ArgumentNullException">Thrown when csvData is null.</exception>
        public static byte[] CompressCsv(string csvData)
        {
            if (csvData == null)
                throw new ArgumentNullException(nameof(csvData));

            using var memoryStream = new MemoryStream();
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            using (var writer = new StreamWriter(gzipStream, Encoding.UTF8))
            {
                writer.Write(csvData);
            }
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Decompresses CSV data using GZip compression.
        ///
        /// This method is used internally to restore the original CSV data
        /// from the compressed byte array stored in CompressedTableCsv.
        ///
        /// Example:
        /// <code>
        /// var originalCsv = PatchTableSheetCompressed.DecompressCsv(compressedData);
        /// // originalCsv now contains the decompressed CSV string
        /// </code>
        /// </summary>
        /// <param name="compressedData">The compressed data to decompress. Must not be null.</param>
        /// <returns>Decompressed CSV string in UTF-8 encoding.</returns>
        /// <exception cref="ArgumentNullException">Thrown when compressedData is null.</exception>
        /// <exception cref="InvalidDataException">Thrown when the compressed data is corrupted or not valid GZip format.</exception>
        public static string DecompressCsv(byte[] compressedData)
        {
            if (compressedData == null)
                throw new ArgumentNullException(nameof(compressedData));

            using var memoryStream = new MemoryStream(compressedData);
            using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Serializes the action data for blockchain storage.
        /// Contains the table name and compressed CSV data.
        /// </summary>
        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .SetItem("table_name", (Text)TableName)
                .SetItem("compressed_table_csv", new Binary(CompressedTableCsv));

        /// <summary>
        /// Deserializes the action data from blockchain storage.
        /// Restores the table name and compressed CSV data.
        /// </summary>
        /// <param name="plainValue">The serialized action data.</param>
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            TableName = (Text) plainValue["table_name"];
            var binary = (Binary) plainValue["compressed_table_csv"];
            CompressedTableCsv = binary.ByteArray.ToArray();
        }
    }
}
