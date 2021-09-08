using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Bencodex;
using Bencodex.Types;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Lib9c.Model.Order;
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
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;
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
            // Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo
            //     .File(new CompactJsonFormatter(), "D:/20210908-2.txt").CreateLogger();
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();
            Libplanet.Crypto.CryptoConfig.CryptoBackend = new Secp256K1CryptoBackend<SHA256>();
            var policySource = new BlockPolicySource(Log.Logger, LogEventLevel.Verbose);
            IBlockPolicy<NCAction> policy =
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

            DateTimeOffset started = DateTimeOffset.UtcNow;
            Block<NCAction> genesis = store.GetBlock<NCAction>(gHash);
            IKeyValueStore stateRootKeyValueStore = new RocksDBKeyValueStore(Path.Combine(storePath, "state_hashes")),
                stateKeyValueStore = new RocksDBKeyValueStore(Path.Combine(storePath, "states"));
            var stateStore = new TrieStateStore(stateKeyValueStore, stateRootKeyValueStore);
            var chain = new BlockChain<NCAction>(policy, stagePolicy, store, stateStore, genesis);
            long height = chain.Tip.Index;
            BlockHash[] blockHashes = limit < 0
                ? chain.BlockHashes.SkipWhile((_, i) => i < height + limit).ToArray()
                : chain.BlockHashes.Take(limit).ToArray();
            Console.Error.WriteLine(
                "Executing {0} blocks: {1}-{2} (inclusive).",
                blockHashes.Length,
                blockHashes[0],
                blockHashes.Last()
            );
            GetRawAvatar(chain);
            // InvalidAvatars(chain);
            // InvalidCount(chain);
            // InvalidLock(chain);
            // InvalidDigest(chain);
            // GetDri(chain);
            GetAvatarV1();
        }

        private static void GetAvatarV1()
        {
            var files = Directory.GetFiles("D:/20210909", "20210909-*.txt", SearchOption.AllDirectories);
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
            var lines = File.ReadAllLines("D:/20210908-2.txt");
            foreach (var line in lines)
            {
                var des = JsonConvert.DeserializeObject<Dictionary<string, string>>(line);
                var aaHex = des!["AvatarAddress"].Substring(2);
                var avatarAddress = new Address(ByteUtil.ParseHex(aaHex));
                var index = long.Parse(des!["DailyRewardReceivedIndex"]);
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
                        File.WriteAllText($"D:/20210909/20210909-{avatarAddress}.txt", data["state"].ToString());
                    }
                ).Wait();
            }
        }

        private static void GetDri(BlockChain<NCAction> chain)
        {
            var lines = File.ReadAllLines("D:/20210908.txt");
            foreach (var line in lines)
            {
                var des = JsonConvert.DeserializeObject<Dictionary<string, string>>(line);
                var aaHex = des!["AvatarAddress"].Substring(2);
                var address = new Address(ByteUtil.ParseHex(aaHex));
                var avatarState = new AvatarState((Dictionary)chain.GetState(address));
                Log.Information(
                    "AgentAddress: {AgentAddress} AvatarAddress: {AvatarAddress}, DailyRewardIndex: {DailyRewardIndex}",
                    avatarState.agentAddress, address, avatarState.dailyRewardReceivedIndex);
            }
        }
        private static void InvalidLock(BlockChain<NCAction> chain)
        {
            var ranking = new RankingState((Dictionary)chain.GetState(Addresses.Ranking));
            var materialItemSheet = new MaterialItemSheet();
            var a = chain.GetState(Addresses.GetSheetAddress<MaterialItemSheet>());
            materialItemSheet.Set(a.ToDotnetString());
            var hourGlassId = TradableMaterial.DeriveTradableId(materialItemSheet.Values
                .First(i => i.ItemSubType == ItemSubType.Hourglass).ItemId);
            var apId = TradableMaterial.DeriveTradableId(materialItemSheet.Values
                .First(i => i.ItemSubType == ItemSubType.ApStone).ItemId);
            foreach (var kv in ranking.RankingMap)
            {
                var avatarAddresses = kv.Value;
                foreach (var avatarAddress in avatarAddresses)
                {
                    Console.WriteLine(avatarAddress);
                    var inventoryAddress = avatarAddress.Derive("inventory");
                    var rawInventory = chain.GetState(inventoryAddress);
                    var avatarState = new AvatarState((Dictionary)chain.GetState(avatarAddress));
                    if (!(rawInventory is null))
                    {
                        avatarState.inventory = new Inventory((List)rawInventory);
                    }

                    var digestListAddress = OrderDigestListState.DeriveAddress(avatarAddress);
                    var rawDigestList = chain.GetState(digestListAddress);
                    if (rawDigestList is null)
                    {
                        continue;
                    }

                    var digestListState = new OrderDigestListState((Dictionary)rawDigestList);
                    avatarState.inventory.UnlockInvalidSlot(digestListState, avatarState.agentAddress, avatarAddress);
                    avatarState.inventory.ReconfigureFungibleItem(digestListState, hourGlassId);
                    avatarState.inventory.ReconfigureFungibleItem(digestListState, apId);
                    var slots = avatarState.inventory.Items.Where(s => s.Locked).ToList();
                    if (!slots.Any())
                    {
                        continue;
                    }

                    foreach (var slot in slots)
                    {
                        if (!slot.Locked)
                        {
                            continue;
                        }

                        var orderLock = (OrderLock)slot.Lock;
                        var orderIds = digestListState.OrderDigestList.Select(d => d.OrderId).ToList();
                        // var orderId = orderLock.OrderId;
                        // var digest = digestListState.OrderDigestList.FirstOrDefault(d => d.OrderId.Equals(orderId));
                        // if (!(digest is null))
                        // {
                        //     var order = OrderFactory.Deserialize((Dictionary)chain.GetState(Order.DeriveAddress(orderId)));
                        //     var itemCount = order is FungibleOrder fungibleOrder ? fungibleOrder.ItemCount : 1;
                        //     var tradableItem  = (ITradableItem)slot.item;
                        //     if (itemCount != slot.count || tradableItem.RequiredBlockIndex != order.ExpiredBlockIndex)
                        //     {
                        //         Log.Information("Invalid ItemCount. orderId: {id} expectedCount: {itemCount} acutalCount: {actualCount}", orderId, itemCount, slot.count);
                        //     }
                        // }
                        // else
                        // {
                        //     Log.Information("Can't find digest: {id}", orderId);
                        // }
                        if (!orderIds.Contains(orderLock.OrderId))
                        {
                            Log.Information("AvatarAddress: {address}, Can't find digest: {id}", avatarAddress, orderLock.OrderId);
                        }
                        // var material = (ITradableItem)slot.item;
                        // avatarState.inventory.ReconfigureFungibleItem(digestListState, material.TradableId);
                    }
                }
            }
        }

        private static void InvalidAvatars(BlockChain<NCAction> chain)
        {
            var ranking = new RankingState((Dictionary)chain.GetState(Addresses.Ranking));
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
                        }
                    }
                }
            }
        }

        private static void InvalidCount(BlockChain<NCAction> chain)
        {
            var ranking = new RankingState((Dictionary)chain.GetState(Addresses.Ranking));
            var materialItemSheet = new MaterialItemSheet();
            var a = chain.GetState(Addresses.GetSheetAddress<MaterialItemSheet>());
            materialItemSheet.Set(a.ToDotnetString());
            var hourGlassId = TradableMaterial.DeriveTradableId(materialItemSheet.Values
                .First(i => i.ItemSubType == ItemSubType.Hourglass).ItemId);
            var apId = TradableMaterial.DeriveTradableId(materialItemSheet.Values
                .First(i => i.ItemSubType == ItemSubType.ApStone).ItemId);
            var itemIds = new[] { hourGlassId, apId };
            foreach (var kv in ranking.RankingMap)
            {
                var avatarAddresses = kv.Value;
                // 6개 삭제되는 케이스 2295997
                // var avatarAddresses = new [] { new Address("CEF7B0817F0CC1a5b43ea3B9a95d887f37526636") };
                foreach (var avatarAddress in avatarAddresses)
                {
                    Console.WriteLine(avatarAddress);
                    var inventoryAddress = avatarAddress.Derive("inventory");
                    var rawInventory = chain.GetState(inventoryAddress);
                    var avatarState = new AvatarState((Dictionary)chain.GetState(avatarAddress));
                    if (!(rawInventory is null))
                    {
                        avatarState.inventory = new Inventory((List)rawInventory);
                    }

                    var digestListAddress = OrderDigestListState.DeriveAddress(avatarAddress);
                    var rawDigestList = chain.GetState(digestListAddress);
                    if (rawDigestList is null)
                    {
                        continue;
                    }

                    var digestListState = new OrderDigestListState((Dictionary)rawDigestList);
                    avatarState.inventory.UnlockInvalidSlot(digestListState, avatarState.agentAddress, avatarAddress);
                    foreach (var itemId in itemIds)
                    {
                        Log.Information("Reconfigure 1");
                        avatarState.inventory.ReconfigureFungibleItem(digestListState, itemId);
                        Log.Information("TestMig");
                        avatarState.inventory.TestMig(digestListState, itemId, chain.Tip.Index);
                        Log.Information("Reconfigure 2");
                        avatarState.inventory.ReconfigureFungibleItem(digestListState, itemId);
                    }

                    var orderIds = digestListState.OrderDigestList.Select(i => i.OrderId).ToList();
                    foreach (var slot in avatarState.inventory.Items.Where(i => i.Locked && i.item is TradableMaterial))
                    {
                        var item = (TradableMaterial)slot.item;
                        var orderId = ((OrderLock)slot.Lock).OrderId;
                        if (!orderIds.Contains(orderId))
                        {
                            throw new Exception($"Invalid Lock. {orderId}");
                        }

                        var order = (FungibleOrder)OrderFactory.Deserialize(
                            (Dictionary)chain.GetState(Order.DeriveAddress(orderId)));
                        if (!avatarState.inventory.TryGetLockedItem(new OrderLock(orderId), out _))
                        {
                            throw new Exception($"Can't find item. {orderId}");
                        }

                        if (item.RequiredBlockIndex != order.ExpiredBlockIndex)
                        {
                            throw new Exception(
                                $"Invalid required blockIndex. {item.RequiredBlockIndex} : {order.ExpiredBlockIndex}");
                        }

                        if (slot.count != order.ItemCount)
                        {
                            throw new Exception($"Invalid count. {slot.count} : {order.ItemCount}");
                        }
                        var shop = new ShardedShopStateV2(
                            (Dictionary)chain.GetState(ShardedShopStateV2.DeriveAddress(order.ItemSubType, orderId)));
                        var od = shop.OrderDigestList.FirstOrDefault(i => i.OrderId.Equals(orderId));
                        if (od is null && order.ExpiredBlockIndex > chain.Tip.Index)
                        {
                            throw new Exception($"Can't find in shop: {orderId}");
                        }
                    }
                }
            }
        }

        private static void InvalidDigest(BlockChain<NCAction> chain)
        {
            for (int i = 0; i < 1; i++)
            {
                var avatarAddresses = new List<Address>
                {
                    new Address("1a155C59a73D024a70Ca9fe280ba719359072170"),
                };
                foreach (var avatarAddress in avatarAddresses)
                {
                    var inventoryAddress = avatarAddress.Derive("inventory");
                    var rawInventory = chain.GetState(inventoryAddress);
                    var avatarState = new AvatarState((Dictionary)chain.GetState(avatarAddress));
                    if (!(rawInventory is null))
                    {
                        avatarState.inventory = new Inventory((List)rawInventory);
                    }

                    var digestListAddress = OrderDigestListState.DeriveAddress(avatarAddress);
                    var rawDigestList = chain.GetState(digestListAddress);
                    if (rawDigestList is null)
                    {
                        continue;
                    }

                    var digestListState = new OrderDigestListState((Dictionary)rawDigestList);
                    var groupBy = digestListState.OrderDigestList
                        .GroupBy(d => new { d.ExpiredBlockIndex, d.TradableId })
                        .OrderByDescending(d => d.Key.ExpiredBlockIndex).ToList();
                    foreach (var group in groupBy)
                    {
                        if (group.Count() == 1)
                        {
                            continue;
                        }
                        var blockIndex = group.Key.ExpiredBlockIndex;
                        foreach (var d in group)
                        {
                            var slot = avatarState.inventory.Items.FirstOrDefault(s =>
                                s.Locked && s.item is TradableMaterial tradableMaterial &&
                                tradableMaterial.RequiredBlockIndex == blockIndex);
                            if (slot is null && chain.Tip.Index - blockIndex > Order.ExpirationInterval)
                            {
                                continue;
                            }
                            var orderLock = (OrderLock)slot.Lock;
                            if (orderLock.OrderId.Equals(d.OrderId))
                            {
                                if (slot.count != d.ItemCount)
                                {
                                    slot.count = d.ItemCount;
                                }
                            }
                            else
                            {
                                var copy = (ITradableFungibleItem) ((ITradableFungibleItem) slot.item).Clone();
                                var clone = new Inventory.Item((ItemBase)copy, d.ItemCount);
                                clone.LockUp(new OrderLock(d.OrderId));
                            }
                        }
                    }
                    var slots = avatarState.inventory.Items.Where(s => s.Locked).ToList();
                    if (!slots.Any())
                    {
                        continue;
                    }

                    foreach (var slot in slots)
                    {
                        var orderLock = (OrderLock)slot.Lock;
                        var orderId = orderLock.OrderId;
                        var digest = digestListState.OrderDigestList.FirstOrDefault(d => d.OrderId.Equals(orderId));
                        // if (!(digest is null))
                        // {
                        //     var order = OrderFactory.Deserialize(
                        //         (Dictionary)chain.GetState(Order.DeriveAddress(orderId)));
                        //     var itemCount = order is FungibleOrder fungibleOrder ? fungibleOrder.ItemCount : 1;
                        //     var tradableItem = (ITradableItem)slot.item;
                        //     if (itemCount != slot.count || tradableItem.RequiredBlockIndex != order.ExpiredBlockIndex)
                        //     {
                        //         Log.Information(
                        //             "Invalid ItemCount. orderId: {id} expectedCount: {itemCount} acutalCount: {actualCount}",
                        //             orderId, itemCount, slot.count);
                        //         // Log.Information("Invalid ItemCount. orderId: {id} expectedCount: {itemCount} acutalCount: {actualCount} expectedIndex: {expectedBlockIndex} actualIndex: {actualBlockIndex}", orderId, itemCount, slot.count, order.ExpiredBlockIndex, tradableItem.RequiredBlockIndex);
                        //     }
                        // }
                        if (digest is null)
                        {
                            avatarState.inventory.RemoveItem(slot);
                        }
                    }
                    Console.WriteLine("Finished");
                }
            }
        }
        //     else
            //     {
            //         // Log.Information("Can't find digest: {id}", orderId);
            //         avatarState.inventory.UnlockInvalidSlot(digestListState, avatarState.agentAddress, avatarAddress);
            //     }
            //     // if (!orderIds.Contains(orderLock.OrderId))
            //     // {
            //     //     inventory.UnlockInvalidSlot(digestListState, avatarState.agentAddress, avatarAddress);
            //     // }
            //     // var material = (ITradableItem)slot.item;
            //     // inventory.ReconfigureFungibleItem(digestListState, material.TradableId);
            // }
            // var slots2 = avatarState.inventory.Items.Where(s => s.Locked).ToList();
            // if (!slots2.Any())
            // {
            //     continue;
            // }
            // avatarState.inventory.UnlockInvalidSlot(digestListState, avatarState.agentAddress, avatarAddress);
            // foreach (var slot in slots2)
            // {
            //     if (!slot.Locked)
            //     {
            //         continue;
            //     }
            //     var orderLock = (OrderLock)slot.Lock;
            //     var orderId = orderLock.OrderId;
            //     var digest = digestListState.OrderDigestList.FirstOrDefault(d => d.OrderId.Equals(orderId));
            //     if (!(digest is null))
            //     {
            //         var order = OrderFactory.Deserialize((Dictionary)chain.GetState(Order.DeriveAddress(orderId)));
            //         var itemCount = order is FungibleOrder fungibleOrder ? fungibleOrder.ItemCount : 1;
            //         var tradableItem  = (ITradableItem)slot.item;
            //         avatarState.inventory.ReconfigureFungibleItem(digestListState, tradableItem.TradableId);
            //         if (itemCount != slot.count)
            //         {
            //             Log.Information("Invalid ItemCount. avatarAddress: {address} orderId: {id} expectedCount: {itemCount} acutalCount: {actualCount} type: {itemType}, {blockIndex}", avatarAddress, orderId, itemCount, slot.count, order.ItemSubType, order.StartedBlockIndex);
            //             Log.Information($"{inventoryAddress}, {chain[order.StartedBlockIndex - 1].Hash}");
            //         }
            //     }
            //     else
            //     {
            //         Log.Information("Can't find digest: {id}", orderId);
            //     }
            // if (!orderIds.Contains(orderLock.OrderId))
            // {
            //     inventory.UnlockInvalidSlot(digestListState, avatarState.agentAddress, avatarAddress);
            // }
            // var material = (ITradableItem)slot.item;
            // inventory.ReconfigureFungibleItem(digestListState, material.TradableId);

            // var addresses = new List<string>()
            // {
            //     "89210CE8d4F5D1D6e750D4Cd2CA342e5D4ad037A",
            //     "558eA927b9449C69577ED1E28a7C14aE0ce550DF",
            //     "9e4405DFd2b25d31a491Fe693f08229cd6490804",
            //     "20367639AF16B592a7BfD1F37220c6E3a1dAE38A",
            //     "DF85d94517828D202641A6C973Cd10F9563BC835",
            //     "2d1b58Fd7661bE5b29525c63F061E006fbe62F1E",
            //     "709E5Bf0823DF4dDFf76a11a6585d1463e9dC9F8",
            //     "31005b6Cbc7f9B2964aED128d248067dD9f77236",
            //     "97C5Ae415dC1FEd33083396A08BfB0f20fb1C788",
            //     "e0b3A31edb32dEA32eb6041a13049cF778a30A12",
            //     "ef7C7674f438291A25DF248272BA296263aE84BB",
            //     "B4F6c2D629D287D0ee8ab847B5Ee5761eC530E4d",
            //     "5234d5884a30dCDeE40A2B0d58162baAe059F629",
            //     "21CEC3F0bBFfe005F7378c27d537659F257718bd",
            //     "1e7453e0523048d3ad9bd9220D45241094F1b40E",
            //     "8Ac7bc2Ab04f576A68b37E3A80D9B9ffB15b1412",
            //     "710d0A7cfcd2A72159cF0233124A1E62Da0be7aC",
            //     "ae7EFb6bE03d4383E8d226b082ad4556c2aE0652",
            //     "1a155C59a73D024a70Ca9fe280ba719359072170",
            // };
            // foreach (var address in addresses)
            // {
            //     var avatarAddress = new Address(ByteUtil.ParseHex(address));
            //     var inventoryAddress = avatarAddress.Derive("inventory");
            //     var rawInventory = chain.GetState(inventoryAddress);
            //     var avatarState = new AvatarState((Dictionary)chain.GetState(avatarAddress));
            //     var inventory = rawInventory is null
            //         ? avatarState.inventory
            //         : new Inventory((List)rawInventory);
            //     var digestListAddress = OrderDigestListState.DeriveAddress(avatarAddress);
            //     var rawDigestList = chain.GetState(digestListAddress);
            //     if (rawDigestList is null)
            //     {
            //         continue;
            //     }
            //     var digestListState = new OrderDigestListState((Dictionary)rawDigestList);
            //     var slots = inventory.Items.Where(i => i.Locked).ToList();
            //     var orderIds = digestListState.OrderDigestList.Select(d => d.OrderId).ToList();
            //     Log.Information(address);
            //     foreach (var slot in slots)
            //     {
            //         var orderLock = (OrderLock)slot.Lock;
            //         var orderId = orderLock.OrderId;
            //         if (!orderIds.Contains(orderId))
            //         {
            //             var order = (FungibleOrder)OrderFactory.Deserialize((Dictionary)chain.GetState(Order.DeriveAddress(orderId)));
            //             Log.Information($"{order.ItemSubType}, {order.ItemCount}");
            //         }
            //     }
            // }

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
