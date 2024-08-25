namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Security.Cryptography;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using Libplanet.Types.Evidence;
    using Libplanet.Types.Tx;

    public class ActionContext : IActionContext
    {
        private IRandom _random = null;

        private IReadOnlyList<ITransaction> _txs = null;

        private IReadOnlyList<EvidenceBase> _evs = null;

        public BlockHash? GenesisHash { get; set; }

        public Address Signer { get; set; }

        public TxId? TxId { get; set; }

        public Address Miner { get; set; }

        public BlockHash BlockHash { get; set; }

        public long BlockIndex { get; set; }

        public int BlockProtocolVersion { get; set; } = BlockMetadata.CurrentProtocolVersion;

        public BlockCommit LastCommit { get; set; }

        public IWorld PreviousState { get; set; }

        public int RandomSeed { get; set; }

        public HashDigest<SHA256>? PreviousStateRootHash { get; set; }

        public bool IsPolicyAction { get; set; }

        public FungibleAssetValue? MaxGasPrice { get; set; }

        public IReadOnlyList<ITransaction> Txs
        {
            get => _txs ?? ImmutableList<ITransaction>.Empty;
            set => _txs = value;
        }

        public IReadOnlyList<EvidenceBase> Evidence
        {
            get => _evs ?? ImmutableList<EvidenceBase>.Empty;
            set => _evs = value;
        }

        public IRandom GetRandom() => _random ?? new TestRandom(RandomSeed);

        // FIXME: Temporary measure to allow inheriting already mutated IRandom.
        public void SetRandom(IRandom random)
        {
            _random = random;
        }
    }
}
