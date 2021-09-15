using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using Bencodex;
using Bencodex.Types;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.RocksDBStore;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Model.State;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
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
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();
            // Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo
            //     .File(new CompactJsonFormatter(), "D:/20210915_avatars.txt").CreateLogger();
            Libplanet.Crypto.CryptoConfig.CryptoBackend = new Secp256K1CryptoBackend<SHA256>();
            var policySource = new BlockPolicySource(Log.Logger, LogEventLevel.Verbose);
            IBlockPolicy<NCAction> policy = new DebugPolicy();
                policySource.GetPolicy(BlockPolicySource.DifficultyBoundDivisor + 1, 10000);
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

            Block<NCAction> genesis = store.GetBlock<NCAction>(gHash);
            IKeyValueStore stateRootKeyValueStore = new RocksDBKeyValueStore(Path.Combine(storePath, "state_hashes")),
                stateKeyValueStore = new RocksDBKeyValueStore(Path.Combine(storePath, "states"));
            var stateStore = new TrieStateStore(stateKeyValueStore, stateRootKeyValueStore);
            var chain = new BlockChain<NCAction>(policy, stagePolicy, store, stateStore, genesis);
            // GetInvalidAvatars(chain);
            // GetInvalidIndex(chain);
            // GetRawAvatar(chain);
            // GetAvatarV1();
            RunMigration(chain);
        }

        private static void GetInvalidAvatars(BlockChain<NCAction> chain)
        {
            var ranking = new RankingState((Dictionary)chain.GetState(Addresses.Ranking));
            var addresses = new List<string>();
            foreach (var kv in ranking.RankingMap)
            {
                var avatarAddresses = kv.Value;
                foreach (var avatarAddress in avatarAddresses)
                {
                    Console.WriteLine(avatarAddress);
                    var inventoryAddress = avatarAddress.Derive("inventory");
                    var wiAddress = avatarAddress.Derive("worldInformation");
                    var qlAddress = avatarAddress.Derive("questList");
                    var rawInventory = chain.GetState(inventoryAddress);
                    var rawWi = chain.GetState(wiAddress);
                    var rawQl = chain.GetState(qlAddress);
                    var avatarState = new AvatarState((Dictionary)chain.GetState(avatarAddress));
                    if (avatarState.inventory is null || avatarState.worldInformation is null ||
                        avatarState.questList is null)
                    {
                        if (rawInventory is null || rawWi is null || rawQl is null)
                        {
                            Log.Information(
                                "AgentAddress: {AgentAddress} AvatarAddress: {AvatarAddress}, inventoryAddress: {InventoryAddress}, worldInformationAddress: {WorldInformationAddress}, questListAddress: {QuestListAddress}, receivedIndex: {DailyRewardReceivedIndex}",
                                avatarState.agentAddress, avatarAddress, inventoryAddress, wiAddress, qlAddress, avatarState.dailyRewardReceivedIndex);
                            addresses.Add(avatarAddress.ToHex());
                        }
                    }
                }
            }
            File.WriteAllLines("D:/20210915_invalid_avatar_list.txt", addresses);
        }

        private static void GetInvalidIndex(BlockChain<NCAction> chain)
        {
            var lines = File.ReadAllLines("D:/20210915_invalid_avatar_list.txt");
            foreach (var line in lines)
            {
                var address = new Address(ByteUtil.ParseHex(line));
                var avatarState = new AvatarState((Dictionary)chain.GetState(address));
                Log.Information(
                    "AgentAddress: {AgentAddress} AvatarAddress: {AvatarAddress}, DailyRewardIndex: {DailyRewardIndex}",
                    avatarState.agentAddress, address, avatarState.dailyRewardReceivedIndex);
            }
        }

        private static void GetAvatarV1()
        {
            var files = Directory.GetFiles("D:/20210915", "20210915-*.txt", SearchOption.AllDirectories);
            var codec = new Codec();
            foreach (var filePath in files)
            {
                var bytes = File.ReadAllText(filePath);
                var iValue = codec.Decode(ByteUtil.ParseHex(bytes));
                var avatarState = new AvatarState((Dictionary)iValue);
                Console.Error.WriteLine($"{avatarState.address}: {avatarState.inventory is null}");
            }
        }

        private static void GetRawAvatar(BlockChain<NCAction> chain)
        {
            var client = new GraphQLHttpClient("https://9c-main-full-state.planetarium.dev/graphql", new NewtonsoftJsonSerializer());
            var lines = File.ReadAllLines("D:/20210915_avatars.txt");
            foreach (var line in lines)
            {
                var des = JsonConvert.DeserializeObject<Dictionary<string, string>>(line);
                var aaHex = des!["AvatarAddress"].Substring(2);
                var avatarAddress = new Address(ByteUtil.ParseHex(aaHex));
                var index = long.Parse(des!["DailyRewardIndex"]);
                var blockHash = chain[index - 1].Hash;

                var request = new GraphQLHttpRequest
                {
                    Query = @"query($address: Address!, $hash: ByteString!) {
  state(address: $address, hash: $hash)
            }",
                    Variables = new
                    {
                        address = avatarAddress.ToHex(),
                        hash = ByteUtil.Hex(blockHash.ByteArray)
                    }
                };
                client.SendQueryAsync<Map>(request, CancellationToken.None).ContinueWith(task =>
                    {
                        var data = task.Result.Data;
                        File.WriteAllText($"D:/20210915/20210915-{avatarAddress}.txt", data["state"].ToString());
                    }
                ).Wait();
            }
        }

        private static void RunMigration(BlockChain<NCAction> chain)
        {
            var rawAction = File.ReadAllText("D:/20210915_migration_avatar_state.txt");
            var codec = new Codec();
            var decoded = (List)codec.Decode(ByteUtil.ParseHex(rawAction));
            Dictionary plainValue = (Dictionary)decoded[1];
            var action = new MigrationAvatarState();
            action.LoadPlainValue(plainValue);
            chain.MakeTransaction(new PrivateKey(), new List<NCAction>
            {
                action
            });
            Log.Information("Start migration");
            chain.MineBlock(new PrivateKey().ToAddress()).Wait();
            foreach (var rawAvatarState in action.avatarStates)
            {
                var address = rawAvatarState["address"].ToAddress();
                var ia = address.Derive("inventory");
                var wa = address.Derive("worldInformation");
                var qa = address.Derive("questList");
                if (chain.GetState(ia) is null || chain.GetState(wa) is null || chain.GetState(qa) is null)
                {
                    throw new Exception(address.ToHex());
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
    }
}
