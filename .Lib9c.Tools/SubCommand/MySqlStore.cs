using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cocona;
using System.Security.Cryptography;
using Libplanet.Action;
using Libplanet.RocksDBStore;
using Libplanet.Tx;
using MySqlConnector;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Lib9c.Tools.SubCommand
{
    public class MySqlStore
    {
        private const string BlockDbName = "block";
        private const string TxDbName = "transaction";
        private const string TxRefDbName = "tx_references";
        private const string SignerRefDbName = "signer_references";
        private const string UpdatedAddressRefDbName = "updated_address_references";
        private StreamWriter blockBulkFile;
        private StreamWriter txBulkFile;
        private StreamWriter txRefBulkFile;
        private StreamWriter signerRefBulkFile;
        private StreamWriter updatedAddressRefBulkFile;
        private string _connectionString;

        [Command(Description = "Migrate monorocksdb store to mysql store.")]
        public async Task Migration(
            [Option('o', Description = "Path to migration target root path.")]
            string originRootPath,
            [Option(
                "mysql-server",
                Description = "A hostname of MySQL server.")]
            string mysqlServer,
            [Option(
                "mysql-port",
                Description = "A port of MySQL server.")]
            uint mysqlPort,
            [Option(
                "mysql-username",
                Description = "The name of MySQL user.")]
            string mysqlUsername,
            [Option(
                "mysql-password",
                Description = "The password of MySQL user.")]
            string mysqlPassword,
            [Option(
                "mysql-database",
                Description = "The name of MySQL database to use.")]
            string mysqlDatabase
            )
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Database = mysqlDatabase,
                UserID = mysqlUsername,
                Password = mysqlPassword,
                Server = mysqlServer,
                Port = mysqlPort,
                AllowLoadLocalInfile = true,
            };

            _connectionString = builder.ConnectionString;

            var originStore = new MonoRocksDBStore(originRootPath, 10000, 10000);
            var totalLength = originStore.CountBlocks();

            string blockFilePath = Path.GetTempFileName();
            blockBulkFile = new StreamWriter(blockFilePath);

            string txFilePath = Path.GetTempFileName();
            txBulkFile = new StreamWriter(txFilePath);

            string txRefFilePath = Path.GetTempFileName();
            txRefBulkFile = new StreamWriter(txRefFilePath);

            string signerRefFilePath = Path.GetTempFileName();
            signerRefBulkFile = new StreamWriter(signerRefFilePath);

            string updatedAddressRefFilePath = Path.GetTempFileName();
            updatedAddressRefBulkFile = new StreamWriter(
                updatedAddressRefFilePath);

            Console.WriteLine("Start preparing block data to load.");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // To see the base mysql schema used for this migration tool,
            // please refer to ../mysql-schema.sql.
            foreach (var item in
                originStore.IterateBlockHashes().Select((value, i) => new {i, value}))
            {
                Console.WriteLine($"block progress: {item.i}/{totalLength}");
                var block = originStore.GetBlock<NCAction>(item.value);
                foreach (var tx in block.Transactions)
                {
                    PutTransaction(tx);
                    StoreTxReferences(tx.Id, block.Hash, tx.Nonce);
                }

                try
                {
                    blockBulkFile.WriteLine(
                        $"{block.Index};" +
                        $"{block.Hash.ToString()};" +
                        $"{block.PreEvaluationHash.ToString()};" +
                        $"{block.StateRootHash?.ToString()};" +
                        $"{block.Difficulty};" +
                        $"{(long)block.TotalDifficulty};" +
                        $"{block.Nonce.ToString()};" +
                        $"{block.Miner?.ToString()};" +
                        $"{block.PreviousHash?.ToString()};" +
                        $"{block.Timestamp.ToString()};" +
                        $"{block.TxHash?.ToString()};" +
                        $"{block.ProtocolVersion}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            Console.WriteLine("Finished block data preparation.");
            sw.Stop();
            Console.WriteLine("Time elapsed: {0:hh\\:mm\\:ss}", sw.Elapsed);

            blockBulkFile.Flush();
            blockBulkFile.Close();
            var blockTask = BulkInsertAsync(BlockDbName, blockFilePath);

            txBulkFile.Flush();
            txBulkFile.Close();
            var txTask = BulkInsertAsync(TxDbName, txFilePath);

            txRefBulkFile.Flush();
            txRefBulkFile.Close();
            var txRefTask = BulkInsertAsync(TxRefDbName, txRefFilePath);

            signerRefBulkFile.Flush();
            signerRefBulkFile.Close();
            var signerRefTask = BulkInsertAsync(SignerRefDbName, signerRefFilePath);

            updatedAddressRefBulkFile.Flush();
            updatedAddressRefBulkFile.Close();
            var updatedAddressRefTask = BulkInsertAsync(
                UpdatedAddressRefDbName,
                updatedAddressRefFilePath);

            var migrationTasks = new List<Task> { blockTask, txTask, txRefTask, signerRefTask, updatedAddressRefTask };
            while (migrationTasks.Count > 0)
            {
                Task finishedTask = await Task.WhenAny(migrationTasks);
                if (finishedTask == blockTask)
                {
                    blockBulkFile.Dispose();
                }
                else if (finishedTask == txTask)
                {
                    txBulkFile.Dispose();
                }
                else if (finishedTask == txRefTask)
                {
                    txRefBulkFile.Dispose();
                }
                else if (finishedTask == signerRefTask)
                {
                    signerRefBulkFile.Dispose();
                }
                else if (finishedTask == updatedAddressRefTask)
                {
                    updatedAddressRefBulkFile.Dispose();
                }

                migrationTasks.Remove(finishedTask);
            }

            originStore.Dispose();
            Console.WriteLine("Migration Complete!");
        }

        private static string Hex(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            string s = BitConverter.ToString(bytes);
            return s.Replace("-", string.Empty).ToLower(CultureInfo.InvariantCulture);
        }

        private void PutTransaction<T>(Transaction<T> tx)
            where T : IAction, new()
        {
            StoreUpdatedAddressReferences(tx);
            StoreSignerReferences(tx.Id, tx.Nonce, tx.Signer);

            try
            {
                txBulkFile.WriteLine(
                    $"{tx.Id.ToString()};" +
                    $"{tx.Nonce};" +
                    $"{tx.Signer.ToString()};" +
                    $"{Hex(tx.Signature)};" +
                    $"{tx.Timestamp.ToString()};" +
                    $"{Hex(tx.PublicKey.Format(true))};" +
                    $"{tx.GenesisHash?.ToString()};" +
                    $"{tx.BytesLength}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void StoreTxReferences(TxId txId, Libplanet.HashDigest<SHA256> blockHash, long txNonce)
        {
            try
            {
                txRefBulkFile.WriteLine(
                    $"{txId.ToString()};" +
                    $"{blockHash.ToString()};" +
                    $"{txNonce}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void StoreSignerReferences(TxId txId, long txNonce, Libplanet.Address signer)
        {
            try
            {
                signerRefBulkFile.WriteLine(
                    $"{signer.ToString()};" +
                    $"{txId.ToHex()};" +
                    $"{txNonce}");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void StoreUpdatedAddressReferences<T>(Transaction<T> tx)
            where T : IAction, new()
        {
            try
            {
                foreach (Libplanet.Address address in tx.UpdatedAddresses)
                {
                    updatedAddressRefBulkFile.WriteLine(
                        $"{address.ToString()};" +
                        $"{tx.Id.ToString()};" +
                        $"{tx.Nonce}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private async Task<int> BulkInsertAsync(
            string tableName,
            string filePath)
        {
            using MySqlConnection connection = new MySqlConnection(_connectionString);
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                Console.WriteLine($"Start bulk load to {tableName}.");
                MySqlBulkLoader loader = new MySqlBulkLoader(connection)
                {
                    TableName = tableName,
                    FileName = filePath,
                    Timeout = 0,
                    LineTerminator = "\n",
                    FieldTerminator = ";",
                    Local = true,
                    ConflictOption = MySqlBulkLoaderConflictOption.Replace,
                };
                var rows = await loader.LoadAsync();
                Console.WriteLine($"Bulk load to {tableName} complete.");
                sw.Stop();
                Console.WriteLine("Time elapsed: {0:hh\\:mm\\:ss}", sw.Elapsed);
                return rows;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine($"Bulk load to {tableName} failed.");
                return 0;
            }
        }
    }
}
