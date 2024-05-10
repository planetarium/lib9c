using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using Nekoyume.Action.DPoS;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Sys;

namespace Nekoyume;

public class DPoSBlockHelper
{
    public static Block ProposeGenesisBlock(
        PrivateKey? privateKey,
        IStateStore stateStore,
        Dictionary<Address, BigInteger> initialNCGs,
        Dictionary<PublicKey, BigInteger> initialValidators)
    {
        privateKey ??= new PrivateKey();
        var trie = stateStore.GetStateRoot(null);
        trie = trie.SetMetadata(new TrieMetadata(BlockMetadata.CurrentProtocolVersion));
        IWorld world = new World(new WorldBaseState(trie, stateStore));
        foreach (var pair in initialNCGs)
        {
            world = world.MintAsset(
                new ActionContext
                {
                    PreviousState = world,
                    Signer = privateKey.Address,
                },
                pair.Key,
                Asset.GovernanceToken * pair.Value);
            world = world.MintAsset(
                new ActionContext
                {
                    PreviousState = world,
                    Signer = privateKey.Address,
                },
                pair.Key,
                Currencies.Mead * 10000);
        }

        foreach (var pair in initialValidators)
        {
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = pair.Key.Address,
            };
            world = new PromoteValidator(pair.Key, Asset.GovernanceToken * pair.Value)
                .Execute(actionContext);
        }

        world = new UpdateValidators().Execute(
            new ActionContext
            {
                PreviousState = world,
                Signer = privateKey.Address
            });
        world = ActionEvaluator.CommitWorld(world, stateStore);
        trie = stateStore.Commit(world.Trie);

        return
            BlockChain.ProposeGenesisBlock(
                privateKey: privateKey,
                stateRootHash: trie.Hash,
                transactions: ImmutableList<Transaction>.Empty,
                timestamp: DateTimeOffset.UtcNow);
    }

    private class ActionContext : IActionContext
    {
        public Address Signer { get; set; }

        public TxId? TxId => null;

        public Address Miner { get; set; }

        public int BlockProtocolVersion => BlockMetadata.CurrentProtocolVersion;

        public BlockCommit? LastCommit => null;

        public long BlockIndex => 0;

        public IWorld PreviousState { get; set; }

        public int RandomSeed { get; set; }

        public bool IsBlockAction => true;

        public FungibleAssetValue? MaxGasPrice => null;

        public IReadOnlyList<ITransaction> Txs => Enumerable.Empty<ITransaction>().ToList();

        public void UseGas(long gas)
        {
        }

        public IRandom GetRandom()
        {
            throw new NotImplementedException();
        }

        public long GasUsed()
        {
            throw new NotImplementedException();
        }

        public long GasLimit()
        {
            throw new NotImplementedException();
        }
    }
}
