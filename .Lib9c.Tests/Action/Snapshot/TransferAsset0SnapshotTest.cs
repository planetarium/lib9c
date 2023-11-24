namespace Lib9c.Tests.Action.Snapshot
{
    using System.Linq;
    using System.Threading.Tasks;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using VerifyTests;
    using VerifyXunit;
    using Xunit;
    using static ActionUtils;

    [UsesVerify]
    public class TransferAsset0SnapshotTest
    {
        public TransferAsset0SnapshotTest()
        {
            VerifierSettings.SortPropertiesAlphabetically();
        }

        [Fact]
        public Task PlainValue()
        {
            var action = new TransferAsset0(
                default(Address),
                default(Address),
                Currency.Legacy("NNN", 2, null) * 100);

            return Verifier
                .Verify(action.PlainValue)
                .UseTypeName((Text)GetActionTypeId<TransferAsset0>());
        }

        [Fact]
        public Task TransferCrystal()
        {
            var senderPrivateKey =
                new PrivateKey(ByteUtil.ParseHex(
                    "810234bc093e2b66406b06dd0c2d2d3320bc5f19caef7acd3f800424bd46cb60"));
            var recipientPrivateKey =
                new PrivateKey(ByteUtil.ParseHex(
                    "f8960846e9ae4ad1c23686f74c8e5f80f22336b6f2175be21db82afa8823c92d"));
            var senderAddress = senderPrivateKey.Address;
            var recipientAddress = recipientPrivateKey.Address;
            var crystal = CrystalCalculator.CRYSTAL;
            var context = new ActionContext();

            var stateStore = new TrieStateStore(new MemoryKeyValueStore());
            var inputTrie = stateStore.GetStateRoot(null);
            IAccount state = new Account(new AccountState(inputTrie))
                .MintAsset(context, senderAddress, crystal * 100);

            // NOTE: Temporary measure using IAccountDelta.
            // Will be removed once Libplanet moves to ITrie based IAccouns.
            inputTrie = inputTrie.Set(state.Delta.ToRawDelta());
            inputTrie = stateStore.Commit(inputTrie);
            state = new Account(new AccountState(inputTrie));

            var actionContext = new ActionContext
            {
                Signer = senderAddress,
                PreviousState = state,
            };
            var action = new TransferAsset0(
                senderAddress,
                recipientAddress,
                crystal * 20);

            var outputState = action.Execute(actionContext);
            var outputTrie = inputTrie.Set(outputState.Delta.ToRawDelta());
            outputTrie = stateStore.Commit(outputTrie);

            var trieDiff = outputTrie.Diff(inputTrie)
                .Select(elem => new object[]
                {
                    ByteUtil.Hex(elem.Path.ByteArray),
                    elem.TargetValue?.ToString(),
                    elem.SourceValue.ToString(),
                })
                .ToArray();
            var accountDiff = AccountDiff.Create(inputTrie, outputTrie);

            // Verifier does not handle tuples well when nested.
            var diff = Verifier
                .Verify(trieDiff)
                .UseTypeName((Text)GetActionTypeId<TransferAsset0>())
                .UseMethodName($"{nameof(TransferCrystal)}.diff");
            return diff;
        }

        [Fact]
        public Task TransferWithMemo()
        {
            var senderPrivateKey =
                new PrivateKey(ByteUtil.ParseHex(
                    "810234bc093e2b66406b06dd0c2d2d3320bc5f19caef7acd3f800424bd46cb60"));
            var recipientPrivateKey =
                new PrivateKey(ByteUtil.ParseHex(
                    "f8960846e9ae4ad1c23686f74c8e5f80f22336b6f2175be21db82afa8823c92d"));
            var senderAddress = senderPrivateKey.Address;
            var recipientAddress = recipientPrivateKey.Address;
            var crystal = CrystalCalculator.CRYSTAL;
            var context = new ActionContext();

            var stateStore = new TrieStateStore(new MemoryKeyValueStore());
            var inputTrie = stateStore.GetStateRoot(null);
            IAccount state = new Account(new AccountState(inputTrie))
                .MintAsset(context, senderAddress, crystal * 100);

            // NOTE: Temporary measure using IAccountDelta.
            // Will be removed once Libplanet moves to ITrie based IAccouns.
            inputTrie = inputTrie.Set(state.Delta.ToRawDelta());
            inputTrie = stateStore.Commit(inputTrie);
            state = new Account(new AccountState(inputTrie));

            var actionContext = new ActionContext
            {
                Signer = senderAddress,
                PreviousState = state,
            };
            var action = new TransferAsset0(
                senderAddress,
                recipientAddress,
                crystal * 20,
                "MEMO");
            var outputState = action.Execute(actionContext);
            var outputTrie = inputTrie.Set(outputState.Delta.ToRawDelta());
            outputTrie = stateStore.Commit(outputTrie);

            var trieDiff = outputTrie.Diff(inputTrie)
                .Select(elem => new object[]
                {
                    ByteUtil.Hex(elem.Path.ByteArray),
                    elem.TargetValue?.ToString(),
                    elem.SourceValue.ToString(),
                })
                .ToArray();

            // Verifier does not handle tuples well when nested.
            var diff = Verifier
                .Verify(trieDiff)
                .UseTypeName((Text)GetActionTypeId<TransferAsset0>())
                .UseMethodName($"{nameof(TransferWithMemo)}.diff");
            return diff;
        }
    }
}
