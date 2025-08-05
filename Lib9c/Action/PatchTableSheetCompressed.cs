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

        public string TableName { get; set; }
        public byte[] CompressedTableCsv { get; set; }

        string IPatchTableSheetCompressedV1.TableName => TableName;
        byte[] IPatchTableSheetCompressedV1.CompressedTableCsv => CompressedTableCsv;

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
        /// </summary>
        /// <param name="csvData">The CSV data to compress.</param>
        /// <returns>Compressed byte array.</returns>
        public static byte[] CompressCsv(string csvData)
        {
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
        /// </summary>
        /// <param name="compressedData">The compressed data to decompress.</param>
        /// <returns>Decompressed CSV string.</returns>
        public static string DecompressCsv(byte[] compressedData)
        {
            using var memoryStream = new MemoryStream(compressedData);
            using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .SetItem("table_name", (Text)TableName)
                .SetItem("compressed_table_csv", new Binary(CompressedTableCsv));

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            TableName = (Text) plainValue["table_name"];
            var binary = (Binary) plainValue["compressed_table_csv"];
            CompressedTableCsv = binary.ByteArray.ToArray();
        }
    }
}
