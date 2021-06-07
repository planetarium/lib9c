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
using Nekoyume;
using Nekoyume.BlockChain;
using Nekoyume.Model.State;
using Serilog;
using Serilog.Events;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Lib9c.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.Error.WriteLine("Too few arguments.");
                Environment.Exit(1);
                return;
            }

            string storePath = args[0];
            string mainPath = args[1];
            long startIndex = Convert.ToInt64(args[2]);
            bool sync = args[3] == "true";
            Log.Logger = new LoggerConfiguration().MinimumLevel.Fatal().WriteTo.Console().CreateLogger();
            Libplanet.Crypto.CryptoConfig.CryptoBackend = new Secp256K1CryptoBackend<SHA256>();
            var policySource = new BlockPolicySource(Serilog.Log.Logger, LogEventLevel.Verbose);
            IBlockPolicy<NCAction> policy =
                policySource.GetPolicy(5000000, 2048);
            IStagePolicy<NCAction> stagePolicy = new VolatileStagePolicy<NCAction>();

            DateTimeOffset started = DateTimeOffset.UtcNow;
            var chain = GetChain(policy, stagePolicy, storePath);
            var mainChain = GetChain(policy, stagePolicy, mainPath);
            if (mainChain.GetState(AuthorizedMinersState.Address) is Dictionary ams &&
                policy is BlockPolicy bp)
            {
                bp.AuthorizedMinersState = new AuthorizedMinersState(ams);
            }

            if (sync)
            {
                Block<NCAction> current = chain.Tip;
                while (!mainChain.ContainsBlock(current.Hash))
                {
                    current = chain[current.PreviousHash.Value];
                }

                var forked = chain.Fork(current.Hash);

                while (forked.Count < mainChain.Count)
                {
                    var block = mainChain[forked.Count];
                    forked.Append(block);
                    Log.Fatal($"BlockIndex: {block.Index} / Remain: {mainChain.Count - forked.Count}");
                }
            }
            DateTimeOffset blocksLoaded = DateTimeOffset.UtcNow;
            long txs = 0;
            long actions = 0;
            for (var i = startIndex; i < chain.Count; i++)
            {
                var block = chain[i];
                if (!block.Transactions.Any(t => t.UpdatedAddresses.Contains(Addresses.Shop)))
                {
                    continue;
                }
                Log.Fatal(
                    "Execute Block #{0} {1}; {2} txs",
                    block.Index,
                    block.Hash,
                    block.Transactions.Count()
                );

                IEnumerable<ActionEvaluation> blockEvals;
                if (block.Index > 0)
                {
                    blockEvals = block.Evaluate(
                        DateTimeOffset.UtcNow,
                        address => chain.GetState(address, block.PreviousHash),
                        (address, currency) => chain.GetBalance(address, currency, block.PreviousHash)
                    );
                }
                else
                {
                    blockEvals = block.Evaluate(
                        DateTimeOffset.UtcNow,
                        _ => null,
                        ((_, currency) => new FungibleAssetValue(currency))
                    );
                }

                try
                {
                    SetStates(chain.Id, chain.StateStore, block, blockEvals.ToArray(), buildStateReferences: true);
                    txs += block.Transactions.LongCount();
                    actions += block.Transactions.Sum(tx => tx.Actions.LongCount()) + 1;
                }
                catch (KeyNotFoundException e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        // Copied from BlockChain<T>.SetStates().
        private static void SetStates(
            Guid chainId,
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

            if (!stateStore.ContainsBlockStates(block.Hash))
            {
                var totalDelta = GetTotalDelta(actionEvaluations, ToStateKey, ToFungibleAssetKey);
                stateStore.SetStates(block, totalDelta);
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

        private static BlockChain<NCAction> GetChain(IBlockPolicy<NCAction> policy, IStagePolicy<NCAction> stagePolicy, string storePath)
        {
            var store = new RocksDBStore(storePath);
            if (!(store.GetCanonicalChainId() is Guid chainId))
            {
                Console.Error.WriteLine("There is no canonical chain: {0}", storePath);
                Environment.Exit(1);
                throw new Exception();
            }

            if (!(store.IndexBlockHash(chainId, 0) is { } gHash))
            {
                Console.Error.WriteLine("There is no genesis block: {0}", storePath);
                Environment.Exit(1);
                throw new Exception();
            }

            Block<NCAction> genesis = store.GetBlock<NCAction>(gHash);
            IKeyValueStore stateRootKeyValueStore = new RocksDBKeyValueStore(Path.Combine(storePath, "state_hashes")),
                stateKeyValueStore = new RocksDBKeyValueStore(Path.Combine(storePath, "states"));
            IStateStore stateStore = new TrieStateStore(stateKeyValueStore, stateRootKeyValueStore);

            var mx = 0L;
            foreach (var cid in store.ListChainIds())
            {
                if (mx < store.CountIndex(cid))
                {
                    mx = store.CountIndex(cid);
                    store.SetCanonicalChainId(cid);
                }
            }

            return new BlockChain<NCAction>(policy, stagePolicy, store, stateStore, genesis);
        }
    }
}
