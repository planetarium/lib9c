using System;
using System.Collections.Immutable;
using Cocona;
using Libplanet.Blocks;
using Libplanet.RocksDBStore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Libplanet;
using Libplanet.Tx;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Lib9c.Tools.SubCommand
{
    public class Query
    {
        private static string SerializeHumanReadable<T>(T target)
        {
            return JsonSerializer.Serialize(target,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters =
                    {
                        new ByteArrayStringJsonConverter()
                    }
                }
            );
        }
        
        private static TxId TxIdFromString(string hex) =>
            new TxId(ByteUtil.ParseHex(hex ?? throw new ArgumentNullException(nameof(hex))));

        public void BlockByIndex(
            [Argument("HOME", Description = "root home path")]
            string home,
            [Argument("BLOCK-INDEX", Description = "block index")]
            int blockIndex
        )
        {
            var store = new RocksDBStore(home);
            var chainId = store.GetCanonicalChainId();
            if (chainId == null)
            {
                Console.Error.WriteLine("cannot find the main branch of the blockchain");
                return;
            }

            var blockHash = store.IndexBlockHash(chainId.Value, blockIndex);
            if (blockHash == null)
            {
                Console.Error.WriteLine($"cannot find the block with the height[{blockIndex}] within the blockchain[{chainId}]");
                return;
            }

            var blockDigest = store.GetBlockDigest(blockHash.Value);
            if (blockDigest == null)
            {
                Console.Error.WriteLine($"cannot find the block with the hash[{blockHash}]");
            }
            Console.WriteLine(SerializeHumanReadable(blockDigest));
        }

        public void BlockByHash(
            [Argument("HOME", Description = "root home path")]
            string home,
            [Argument("BLOCK-HASH", Description = "block hash")]
            string blockHash
        )
        {
            var store = new RocksDBStore(home);
            var blockDigest = store.GetBlockDigest(BlockHash.FromString(blockHash));
            if (blockDigest == null)
            {
                Console.Error.WriteLine($"cannot find the block with the hash[{blockHash}]");
                return;
            }
            Console.WriteLine(SerializeHumanReadable(blockDigest));
        }
        
        public void TxById(
            [Argument("HOME", Description = "root home path")]
            string home,
            [Argument("TX-ID", Description = "tx id")]
            string strTxId
        )
        {
            var store = new RocksDBStore(home);
            var txId = TxIdFromString(strTxId);
            var tx = store.GetTransaction<NCAction>(txId);
            if (tx == null)
            {
                Console.Error.WriteLine($"cannot find the tx with the tx id[{strTxId}]");
                return;
            }
            Console.WriteLine(SerializeHumanReadable(tx));
        }
    }

    public class ByteArrayStringJsonConverter : JsonConverter<ImmutableArray<byte>>
    {
        public override ImmutableArray<byte> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var hexString = reader.GetString();
            return ImmutableArray.Create(ByteUtil.ParseHex(hexString));
        }

        public override void Write(Utf8JsonWriter writer, ImmutableArray<byte> value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(ByteUtil.Hex(value));
        }
    }
}