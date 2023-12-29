namespace Nekoyume.Blockchain
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Crypto;
    using Libplanet.Types.Tx;

    public class NCStagePolicy : IStagePolicy
    {
        private readonly VolatileStagePolicy _impl;
        private readonly ConcurrentDictionary<Address, SortedList<Transaction, TxId>> _txs;
        private readonly int _quotaPerSigner;
        private readonly ConcurrentDictionary<Address, int> _quotaPerSignerList;
        private IAccessControlService? _accessControlService;

        public NCStagePolicy(TimeSpan txLifeTime, int quotaPerSigner, IAccessControlService? accessControlService = null)
        {
            if (quotaPerSigner < 1)
            {
                throw new ArgumentOutOfRangeException(
                    $"{nameof(quotaPerSigner)} must be positive: ${quotaPerSigner}");
            }

            _txs = new ConcurrentDictionary<Address, SortedList<Transaction, TxId>>();
            _quotaPerSigner = quotaPerSigner;
            _impl = (txLifeTime == default)
                ? new VolatileStagePolicy()
                : new VolatileStagePolicy(txLifeTime);

            _quotaPerSignerList = new ConcurrentDictionary<Address, int>();
            _accessControlService = accessControlService;
        }

        public Transaction Get(BlockChain blockChain, TxId id, bool filtered = true)
            => _impl.Get(blockChain, id, filtered);

        public long GetNextTxNonce(BlockChain blockChain, Address address)
            => _impl.GetNextTxNonce(blockChain, address);

        public void Ignore(BlockChain blockChain, TxId id)
            => _impl.Ignore(blockChain, id);

        public bool Ignores(BlockChain blockChain, TxId id)
            => _impl.Ignores(blockChain, id);

        public IEnumerable<Transaction> Iterate(BlockChain blockChain, bool filtered = true)
        {
            if (filtered)
            {
                var txsPerSigner = new ConcurrentDictionary<Address, SortedSet<Transaction>>();
                foreach (Transaction tx in _impl.Iterate(blockChain, filtered))
                {
                    if (!txsPerSigner.TryGetValue(tx.Signer, out var s))
                    {
                        txsPerSigner[tx.Signer] = s = new SortedSet<Transaction>(new TxComparer());
                    }

                    s.Add(tx);
                    int txQuotaPerSigner = _quotaPerSigner;

                    // update txQuotaPerSigner if signer is in the list
                    if (_quotaPerSignerList.TryGetValue(tx.Signer, out int signerQuota))
                    {
                        txQuotaPerSigner = signerQuota;
                    }


                    if (s.Count > txQuotaPerSigner)
                    {
                        s.Remove(s.Max);
                    }
                }

#pragma warning disable LAA1002 // DictionariesOrSetsShouldBeOrderedToEnumerate
                return txsPerSigner.Values.SelectMany(i => i);
#pragma warning restore LAA1002 // DictionariesOrSetsShouldBeOrderedToEnumerate
            }
            else
            {
                return _impl.Iterate(blockChain, filtered);
            }
        }

        public bool Stage(BlockChain blockChain, Transaction transaction)
        {
            if (_accessControlService?.GetTxQuota(transaction.Signer) is { } acsTxQuota)
            {
                _quotaPerSignerList[transaction.Signer] = acsTxQuota;

                if (acsTxQuota == 0)
                {
                    return false;
                }
            }

            var deniedTxs = new[]
            {
                // CreatePledge Transaction with 50000 addresses
                TxId.FromHex("300826da62b595d8cd663dadf04995a7411534d1cdc17dac75ce88754472f774"),
                // CreatePledge Transaction with 5000 addresses
                TxId.FromHex("210d1374d8f068de657de6b991e63888da9cadbc68e505ac917b35568b5340f8"),
            };
            if (deniedTxs.Contains(transaction.Id))
            {
                return false;
            }

            return _impl.Stage(blockChain, transaction);
        }

        public bool Unstage(BlockChain blockChain, TxId id)
            => _impl.Unstage(blockChain, id);

        private class TxComparer : IComparer<Transaction>
        {
            public int Compare(Transaction x, Transaction y)
            {
                if (x.Nonce < y.Nonce)
                {
                    return -1;
                }
                else if (x.Nonce > y.Nonce)
                {
                    return 1;
                }
                else if (x.Timestamp < y.Timestamp)
                {
                    return -1;
                }
                else if (x.Timestamp > y.Timestamp)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}
