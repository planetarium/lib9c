using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.RocksDBStore;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Nekoyume.BlockChain;
using Nekoyume.BlockChain.Policy;
using Serilog;
using Serilog.Events;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

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
            var policySource = new BlockPolicySource(Log.Logger, LogEventLevel.Verbose);
            IBlockPolicy<NCAction> policy =
                policySource.GetPolicy(
                    // Explicitly set to lowest possible difficulty.
                    minimumDifficulty: BlockPolicySource.DifficultyStability,
                    maxTransactionsBytesPolicy: null,
                    minTransactionsPerBlockPolicy: null,
                    maxTransactionsPerBlockPolicy: null,
                    maxTransactionsPerSignerPerBlockPolicy: null,
                    authorizedMinersPolicy: null,
                    permissionedMinersPolicy: null,
                    minBlockProtocolVersionPolicy: null,
                    validatorAdminPolicy: null);
            IStagePolicy<NCAction> stagePolicy = new VolatileStagePolicy<NCAction>();
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
            Block<NCAction> genesis = store.GetBlock<NCAction>(gHash);
            IKeyValueStore stateKeyValueStore = new RocksDBKeyValueStore(Path.Combine(storePath, "states"));
            var stateStore = new TrieStateStore(stateKeyValueStore);
            var chain = new BlockChain<NCAction>(policy, stagePolicy, store, stateStore, genesis);
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
            Block<NCAction>[] blocks = blockHashes.Select(h => chain[h]).ToArray();
            DateTimeOffset blocksLoaded = DateTimeOffset.UtcNow;
            long txs = 0;
            long actions = 0;
            foreach (Block<NCAction> block in blocks)
            {
                Console.Error.WriteLine(
                    "Block #{0} {1}; {2} txs",
                    block.Index,
                    block.Hash,
                    block.Transactions.Count()
                );

                IEnumerable<ActionEvaluation> blockEvals =
                chain.ExecuteActions(block, StateCompleterSet<NCAction>.Reject);
                SetStates(
                    chain.Id,
                    store,
                    stateStore,
                    block,
                    blockEvals.ToArray(),
                    buildStateReferences: true
                );
                txs += block.Transactions.LongCount();
                actions += block.Transactions.Sum(tx =>
                    (tx.CustomActions is { } customActions ? customActions.LongCount() : 0) +
                    (tx.SystemAction is { } systemActions ? 1L : 0L)) + 1;
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

        // Copied from BlockChain<T>.SetStates().
        private static void SetStates(
            Guid chainId,
            IStore store,
            IStateStore stateStore,
            Block<NCAction> block,
            IReadOnlyList<ActionEvaluation> actionEvaluations,
            bool buildStateReferences
        )
        {
            IImmutableSet<Address> stateUpdatedAddresses = actionEvaluations
                .SelectMany(a => a.OutputStates.StateUpdatedAddresses)
                .ToImmutableHashSet();
            IImmutableSet<(Address, Currency)> updatedFungibleAssets = actionEvaluations
                .SelectMany(a => a.OutputStates.UpdatedFungibleAssets
                    .SelectMany(kv => kv.Value.Select(c => (kv.Key, c))))
                .ToImmutableHashSet();

            if (!stateStore.ContainsStateRoot(block.StateRootHash))
            {
                HashDigest<SHA256>? prevStateRootHash = store.GetStateRootHash(block.PreviousHash);
                var totalDelta = GetTotalDelta(actionEvaluations, ToStateKey, ToFungibleAssetKey);
                stateStore.Commit(prevStateRootHash, totalDelta);
            }
        }

        // Copied from ActionEvaluationsExtensions.GetTotalDelta().
        private static ImmutableDictionary<string, IValue> GetTotalDelta(
            IReadOnlyList<ActionEvaluation> actionEvaluations,
            Func<Address, string> toStateKey,
            Func<(Address, Currency), string> toFungibleAssetKey)
        {
            IImmutableSet<Address> stateUpdatedAddresses = actionEvaluations
                .SelectMany(a => a.OutputStates.StateUpdatedAddresses)
                .ToImmutableHashSet();
            IImmutableSet<(Address, Currency)> updatedFungibleAssets = actionEvaluations
                .SelectMany(a => a.OutputStates.UpdatedFungibleAssets
                    .SelectMany(kv => kv.Value.Select(c => (kv.Key, c))))
                .ToImmutableHashSet();

            IAccountStateDelta lastStates = actionEvaluations.Count > 0
                ? actionEvaluations[actionEvaluations.Count - 1].OutputStates
                : null;
            ImmutableDictionary<string, IValue> totalDelta =
                stateUpdatedAddresses.ToImmutableDictionary(
                    toStateKey,
                    a => lastStates?.GetState(a)
                ).SetItems(
                    updatedFungibleAssets.Select(pair =>
                        new KeyValuePair<string, IValue>(
                            toFungibleAssetKey(pair),
                            new Bencodex.Types.Integer(
                                lastStates?.GetBalance(pair.Item1, pair.Item2).RawValue ?? 0
                            )
                        )
                    )
                );

            return totalDelta;
        }

        // Copied from KeyConverters.ToStateKey().
        private static string ToStateKey(Address address) => address.ToHex().ToLowerInvariant();

        // Copied from KeyConverters.ToFungibleAssetKey().
        private static string ToFungibleAssetKey(Address address, Currency currency) =>
            "_" + address.ToHex().ToLowerInvariant() +
            "_" + ByteUtil.Hex(currency.Hash.ByteArray).ToLowerInvariant();

        // Copied from KeyConverters.ToFungibleAssetKey().
        private static string ToFungibleAssetKey((Address, Currency) pair) =>
            ToFungibleAssetKey(pair.Item1, pair.Item2);
    }
}
