using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.RocksDBStore;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Libplanet.Types.Blocks;
using Nekoyume.Action.Loader;
using Nekoyume.Blockchain;
using Nekoyume.Blockchain.Policy;
using Serilog;

namespace Lib9c.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Too few arguments.");
                Environment.Exit(1);
                return;
            }

            string storePath = args[0];
            int limit = int.Parse(args[1]);
            int offset = 0;

            if (args.Length >= 3)
            {
                offset = int.Parse(args[2]);
            }

            if (limit < 0)
            {
                Console.Error.WriteLine("Limit value must be greater than 0. Entered value: {0}", limit);
                Environment.Exit(1);
                return;
            }

            if (offset < 0)
            {
                Console.Error.WriteLine("Offset value must be greater than 0. Entered value: {0}", offset);
                Environment.Exit(1);
                return;
            }

            Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Console().CreateLogger();
            Libplanet.Crypto.CryptoConfig.CryptoBackend = new Secp256K1CryptoBackend<SHA256>();
            var policySource = new BlockPolicySource();
            IBlockPolicy policy =
                policySource.GetPolicy(
                    maxTransactionsBytesPolicy: null,
                    minTransactionsPerBlockPolicy: null,
                    maxTransactionsPerBlockPolicy: null,
                    maxTransactionsPerSignerPerBlockPolicy: null);
            IStagePolicy stagePolicy = new VolatileStagePolicy();
            var store = new RocksDBStore(storePath);
            if (!(store.GetCanonicalChainId() is Guid chainId))
            {
                Console.Error.WriteLine("There is no canonical chain: {0}", storePath);
                Environment.Exit(1);
                return;
            }

            if (!(store.IndexBlockHash(chainId, 0) is { } gHash))
            {
                Console.Error.WriteLine("There is no genesis block: {0}", storePath);
                Environment.Exit(1);
                return;
            }

            DateTimeOffset started = DateTimeOffset.UtcNow;
            Block genesis = store.GetBlock(gHash);
            IKeyValueStore stateKeyValueStore = new RocksDBKeyValueStore(Path.Combine(storePath, "states"));
            var stateStore = new TrieStateStore(stateKeyValueStore);
            var actionEvaluator = new ActionEvaluator(
                new PolicyActionsRegistry(
                    _ => policy.BeginBlockActions,
                    _ => policy.EndBlockActions,
                    _ => policy.BeginTxActions,
                    _ => policy.EndTxActions),
                stateStore,
                new NCActionLoader());
            var chain = new BlockChain(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                new BlockChainStates(store, stateStore),
                actionEvaluator);
            long height = chain.Tip.Index;
            if (offset + limit > (int)height)
            {
                Console.Error.WriteLine(
                    "The sum of the offset and limit is greater than the chain tip index: {0}", height);
                Environment.Exit(1);
                return;
            }

            BlockHash[] blockHashes = store.IterateIndexes(chain.Id, offset, limit).Select((value, i) => value ).ToArray();
            Console.Error.WriteLine(
                "Executing {0} blocks: {1}-{2} (inclusive).",
                blockHashes.Length,
                blockHashes[0],
                blockHashes.Last()
            );
            Block[] blocks = blockHashes.Select(h => chain[h]).ToArray();
            DateTimeOffset blocksLoaded = DateTimeOffset.UtcNow;
            long txs = 0;
            long actions = 0;
            foreach (Block block in blocks)
            {
                Console.Error.WriteLine(
                    "Block #{0} {1}; {2} txs",
                    block.Index,
                    block.Hash,
                    block.Transactions.Count()
                );

                chain.DetermineNextBlockStateRootHash(block, out IReadOnlyList<ICommittedActionEvaluation> blockEvals);
                txs += block.Transactions.LongCount();
                actions += block.Transactions.Sum(tx =>
                    tx.Actions is { } customActions ? customActions.LongCount() : 0);
            }

            DateTimeOffset ended = DateTimeOffset.UtcNow;
            Console.WriteLine("Loading blocks\t{0}", blocksLoaded - started);
            long execActionsTotalMilliseconds = (long) (ended - blocksLoaded).TotalMilliseconds;
            Console.WriteLine("Executing actions\t{0}ms", execActionsTotalMilliseconds);
            Console.WriteLine("Average per block\t{0}ms", execActionsTotalMilliseconds / blockHashes.Length);
            Console.WriteLine("Average per tx\t{0}ms", execActionsTotalMilliseconds / txs);
            Console.WriteLine("Average per action\t{0}ms", execActionsTotalMilliseconds / actions);
            Console.WriteLine("Total elapsed\t{0}", ended - started);
        }
    }
}
