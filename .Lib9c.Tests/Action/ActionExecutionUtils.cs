namespace Lib9c.Tests.Action
{
    using System.Collections.Immutable;
    using System.Security.Cryptography;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Blockchain;
    using Libplanet.Store;
    using Libplanet.Store.Trie;

    public static class ActionExecutionUtils
    {
        public static HashDigest<SHA256> CalculateStateRootHash<T>(
            T action, IAccountStateDelta previousStates = null, Address? signer = null, int? randomSeed = null)
            where T : IAction, new()
        {
            var address = default(Address);
            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = previousStates ?? new State(ImmutableDictionary<Address, IValue>.Empty),
                Signer = signer ?? address,
                Miner = address,
                Random = new Random(randomSeed ?? 0),
                BlockIndex = 1,
            });
            var stateStore = new TrieStateStore(new DefaultKeyValueStore(null), new DefaultKeyValueStore(null));
            IImmutableDictionary<string, IValue> delta = nextState.GetTotalDelta(ToStateKey, ToFungibleAssetKey);

            var genesis = BlockChain<T>.MakeGenesisBlock();
            stateStore.SetStates(genesis, delta);
            HashDigest<SHA256> stateRootHash = stateStore.GetRootHash(genesis.Hash);
            return stateRootHash;
        }

        internal static string ToStateKey(Address address) => address.ToHex().ToLowerInvariant();

        internal static string ToFungibleAssetKey(Address address, Currency currency) =>
            "_" + address.ToHex().ToLowerInvariant() +
            "_" + ByteUtil.Hex(currency.Hash.ByteArray).ToLowerInvariant();

        internal static string ToFungibleAssetKey((Address, Currency) pair) =>
            ToFungibleAssetKey(pair.Item1, pair.Item2);
    }
}
