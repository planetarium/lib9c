#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Bencodex.Types;
using Lib9c.Renderers;
using Libplanet.Action;
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
using Nekoyume.Action.Guild;
using Nekoyume.Action.Loader;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.Blockchain.Policy;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class MorgageTest
{
    public const int Timeout = 30000;
    private readonly PrivateKey _privateKey = new PrivateKey();

    [Fact(Timeout = Timeout)]
    public void Execute()
    {
        var validatorKey = new PrivateKey();
        var validatorAddress = validatorKey.Address;
        var signerKey = new PrivateKey();

        var blockPolicySource = new BlockPolicySource();
        var policy = blockPolicySource.GetPolicy(
            maxTransactionsBytesPolicy: null!,
            minTransactionsPerBlockPolicy: null!,
            maxTransactionsPerBlockPolicy: null!,
            maxTransactionsPerSignerPerBlockPolicy: null!);
        var stagePolicy = new VolatileStagePolicy();
        var validator = new Validator(validatorKey.PublicKey, 10_000_000_000_000_000_000);
        var genesis = MakeGenesisBlock(
            new ValidatorSet(new List<Validator> { validator }),
            validatorAddress,
            ImmutableHashSet.Create(validatorAddress));
        using var store = new MemoryStore();
        using var keyValueStore = new MemoryKeyValueStore();
        using var stateStore = new TrieStateStore(keyValueStore);
        var actionEvaluator = new ActionEvaluator(
            policy.PolicyActionsRegistry,
            stateStore: stateStore,
            actionTypeLoader: new NCActionLoader());
        var actionRenderer = new ActionRenderer();

        var blockChain = BlockChain.Create(
            policy,
            stagePolicy,
            store,
            stateStore,
            genesis,
            actionEvaluator,
            renderers: new[] { actionRenderer });

        var mintAmount = 0 * Currencies.Mead;
        var mint = new PrepareRewardAssets
        {
            RewardPoolAddress = validatorAddress,
            Assets = new List<FungibleAssetValue>
            {
                mintAmount,
            },
        };

        blockChain.MakeTransaction(
            validatorKey,
            new ActionBase[] { mint, });

        Block block = blockChain.ProposeBlock(validatorKey);
        var worldState = blockChain.GetNextWorldState() ?? throw new InvalidOperationException();
        var validatorSet = worldState.GetValidatorSet();
        BlockCommit commit = GenerateBlockCommit(
            block,
            validatorSet,
            new PrivateKey[] { validatorKey });

        actionRenderer.Reset();
        blockChain.Append(block, commit);
        actionRenderer.Wait(1000);

        worldState = blockChain.GetNextWorldState() ?? throw new InvalidOperationException();
        var mead2 = worldState.GetBalance(validatorKey.Address, Currencies.Mead);

        blockChain.MakeTransaction(
            signerKey,
            new ActionBase[] { new MakeGuild(validatorAddress), },
            maxGasPrice: Currencies.Mead * 1,
            gasLimit: 1);

        block = blockChain.ProposeBlock(validatorKey, commit);
        worldState = blockChain.GetNextWorldState() ?? throw new InvalidOperationException();
        validatorSet = worldState.GetValidatorSet();
        commit = GenerateBlockCommit(
            block,
            validatorSet,
            new PrivateKey[] { validatorKey });

        actionRenderer.Reset();
        blockChain.Append(block, commit);
        Assert.True(actionRenderer.Wait(1000));

        var exceptions = actionRenderer.Exceptions;

        var mead3 = blockChain.GetWorldState(blockChain.Tip.Hash).GetBalance(validatorKey.Address, Currencies.Mead);

        Assert.NotEmpty(exceptions);

        worldState = blockChain.GetNextWorldState() ?? throw new InvalidOperationException();

        var actualBalance = worldState.GetBalance(signerKey.Address, Currencies.Mead);
        Assert.Equal(Currencies.Mead * 0, actualBalance);
    }

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

    private Block MakeGenesisBlock(
        ValidatorSet validators,
        Address adminAddress,
        IImmutableSet<Address> activatedAddresses)
    {
        var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        (ActivationKey activationKey, PendingActivationState pendingActivation) =
            ActivationKey.Create(_privateKey, nonce);
        var pendingActivations = new PendingActivationState[] { pendingActivation };

        var sheets = TableSheetsImporter.ImportSheets();
        return BlockHelper.ProposeGenesisBlock(
            validators,
            sheets,
            new GoldDistribution[0],
            pendingActivations);
    }

    private Block EvaluateAndSign(
        HashDigest<SHA256> stateRootHash,
        PreEvaluationBlock preEvaluationBlock,
        PrivateKey privateKey
    )
    {
        if (preEvaluationBlock.Index < 1)
        {
            throw new ArgumentException(
                $"Given {nameof(preEvaluationBlock)} must have block index " +
                $"higher than 0");
        }

        if (preEvaluationBlock.ProtocolVersion < BlockMetadata.SlothProtocolVersion)
        {
            throw new ArgumentException(
                $"{nameof(preEvaluationBlock)} of which protocol version less than" +
                $"{BlockMetadata.SlothProtocolVersion} is not acceptable");
        }

        return preEvaluationBlock.Sign(privateKey, stateRootHash);
    }

    private sealed class ActionRenderer : IActionRenderer
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

        public bool Wait(int timeout)
        {
            return _resetEvent.WaitOne(timeout);
        }
    }
}
