#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.Loader;
using Libplanet.Action.State;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blockchain.Renderers;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Action.Loader;
using Nekoyume.Blockchain.Policy;
using Nekoyume.Model;
using Nekoyume.Model.State;

public abstract class TxAcitonTestBase
{
    protected static readonly Currency Mead = Currencies.Mead;
    protected static readonly Currency NCG = Currency.Legacy("NCG", 2, null);
    private readonly PrivateKey _privateKey = new PrivateKey();
    private BlockCommit? _lastCommit;

    protected TxAcitonTestBase()
    {
        var validatorKey = new PrivateKey();

        var blockPolicySource = new BlockPolicySource(
            actionLoader: new GasActionLoader());
        var policy = blockPolicySource.GetPolicy(
            maxTransactionsBytesPolicy: null!,
            minTransactionsPerBlockPolicy: null!,
            maxTransactionsPerBlockPolicy: null!,
            maxTransactionsPerSignerPerBlockPolicy: null!);
        var stagePolicy = new VolatileStagePolicy();
        var validator = new Validator(validatorKey.PublicKey, 10_000_000_000_000_000_000);
        var genesis = MakeGenesisBlock(
            new ValidatorSet(new List<Validator> { validator }));
        using var store = new MemoryStore();
        using var keyValueStore = new MemoryKeyValueStore();
        using var stateStore = new TrieStateStore(keyValueStore);
        var actionEvaluator = new ActionEvaluator(
            policy.PolicyActionsRegistry,
            stateStore: stateStore,
            actionTypeLoader: new GasActionLoader());
        var actionRenderer = new ActionRenderer();

        var blockChain = BlockChain.Create(
            policy,
            stagePolicy,
            store,
            stateStore,
            genesis,
            actionEvaluator,
            renderers: new[] { actionRenderer });

        BlockChain = blockChain;
        Renderer = actionRenderer;
        ValidatorKey = validatorKey;
    }

    protected BlockChain BlockChain { get; }

    protected ActionRenderer Renderer { get; }

    protected PrivateKey ValidatorKey { get; }

    protected void EnsureToMintAsset(PrivateKey privateKey, FungibleAssetValue fav)
    {
        var prepareRewardAssets = new PrepareRewardAssets
        {
            RewardPoolAddress = privateKey.Address,
            Assets = new List<FungibleAssetValue>
            {
                fav,
            },
        };
        var actions = new ActionBase[] { prepareRewardAssets, };

        Renderer.Reset();
        MakeTransaction(privateKey, actions);
        MoveToNextBlock();
        Renderer.Wait();
    }

    protected void MoveToNextBlock(bool throwOnError = false)
    {
        var blockChain = BlockChain;
        var lastCommit = _lastCommit;
        var validatorKey = ValidatorKey;
        var block = blockChain.ProposeBlock(validatorKey, lastCommit);
        var worldState = blockChain.GetNextWorldState()
            ?? throw new InvalidOperationException("Failed to get next world state");
        var validatorSet = worldState.GetValidatorSet();
        var blockCommit = GenerateBlockCommit(
            block, validatorSet, new PrivateKey[] { validatorKey });

        Renderer.Reset();
        blockChain.Append(block, blockCommit);
        Renderer.Wait();
        if (throwOnError && Renderer.Exceptions.Any())
        {
            throw new AggregateException(Renderer.Exceptions);
        }

        _lastCommit = blockCommit;
    }

    protected IWorldState GetNextWorldState()
    {
        var blockChain = BlockChain;
        return blockChain.GetNextWorldState()
            ?? throw new InvalidOperationException("Failed to get next world state");
    }

    protected void MakeTransaction(
        PrivateKey privateKey,
        IEnumerable<IAction> actions,
        FungibleAssetValue? maxGasPrice = null,
        long? gasLimit = null,
        DateTimeOffset? timestamp = null)
    {
        var blockChain = BlockChain;
        blockChain.MakeTransaction(
            privateKey, actions, maxGasPrice, gasLimit, timestamp);
    }

    protected FungibleAssetValue GetBalance(Address address, Currency currency)
        => GetNextWorldState().GetBalance(address, currency);

    private BlockCommit GenerateBlockCommit(
        Block block, ValidatorSet validatorSet, IEnumerable<PrivateKey> validatorPrivateKeys)
    {
        return block.Index != 0
            ? new BlockCommit(
                block.Index,
                0,
                block.Hash,
                validatorPrivateKeys.Select(k => new VoteMetadata(
                    block.Index,
                    0,
                    block.Hash,
                    DateTimeOffset.UtcNow,
                    k.PublicKey,
                    validatorSet.GetValidator(k.PublicKey).Power,
                    VoteFlag.PreCommit).Sign(k)).ToImmutableArray())
            : throw new InvalidOperationException("Block index must be greater than 0");
    }

    private Block MakeGenesisBlock(ValidatorSet validators)
    {
        var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        (ActivationKey _, PendingActivationState pendingActivation) =
            ActivationKey.Create(_privateKey, nonce);
        var pendingActivations = new PendingActivationState[] { pendingActivation };

        var sheets = TableSheetsImporter.ImportSheets();
        return BlockHelper.ProposeGenesisBlock(
            validators,
            sheets,
            new GoldDistribution[0],
            pendingActivations);
    }

    protected sealed class ActionRenderer : IActionRenderer
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        private List<Exception> _exceptionList = new List<Exception>();

        public Exception[] Exceptions => _exceptionList.ToArray();

        public void RenderAction(IValue action, ICommittedActionContext context, HashDigest<SHA256> nextState)
        {
        }

        public void RenderActionError(IValue action, ICommittedActionContext context, Exception exception)
        {
            _exceptionList.Add(exception);
        }

        public void RenderBlock(Block oldTip, Block newTip)
        {
            _exceptionList.Clear();
        }

        public void RenderBlockEnd(Block oldTip, Block newTip)
        {
            _resetEvent.Set();
        }

        public void Reset() => _resetEvent.Reset();

        public void Wait(int timeout)
        {
            if (!_resetEvent.WaitOne(timeout))
            {
                throw new TimeoutException("Timeout");
            }
        }

        public void Wait() => Wait(10000);
    }

    [ActionType(TypeIdentifier)]
    protected class GasAction : ActionBase
    {
        public const string TypeIdentifier = "gas_action";

        public GasAction()
        {
        }

        public long Consumption { get; set; }

        public override IValue PlainValue => Dictionary.Empty
           .Add("type_id", TypeIdentifier)
           .Add("consumption", new Integer(Consumption));

        public override void LoadPlainValue(IValue plainValue)
        {
            if (plainValue is not Dictionary root ||
                !root.TryGetValue((Text)"consumption", out var rawValues) ||
                rawValues is not Integer value)
            {
                throw new InvalidCastException();
            }

            Consumption = (long)value.Value;
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(Consumption);
            return context.PreviousState;
        }
    }

    protected class GasActionLoader : IActionLoader
    {
        private readonly NCActionLoader _actionLoader;

        public GasActionLoader()
        {
            _actionLoader = new NCActionLoader();
        }

        public IAction LoadAction(long index, IValue value)
        {
            if (value is Dictionary pv &&
                    pv.TryGetValue((Text)"type_id", out IValue rawTypeId) &&
                    rawTypeId is Text typeId && typeId == GasAction.TypeIdentifier)
            {
                var action = new GasAction();
                action.LoadPlainValue(pv);
                return action;
            }

            return _actionLoader.LoadAction(index, value);
        }
    }
}
