namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Lib9c.Renderers;
    using Lib9c.Tests.TestHelper;
    using Libplanet.Action;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Crypto;
    using Libplanet.Types.Tx;
    using Nekoyume.Action;
    using Nekoyume.Blockchain;
    using Nekoyume.Blockchain.Policy;
    using Xunit;

    public class StagePolicyTest
    {
        private readonly PrivateKey[] _accounts;

        private readonly Dictionary<Address, Transaction[]> _txs;

        public StagePolicyTest()
        {
            _accounts = new[]
            {
                new PrivateKey(),
                new PrivateKey(),
                new PrivateKey(),
                new PrivateKey(),
            };
            _txs = _accounts.ToDictionary(
                acc => acc.Address,
                acc => Enumerable
                    .Range(0, 10)
                    .Select(
                        n => Transaction.Create(
                            n,
                            acc,
                            default,
                            new ActionBase[0].ToPlainValues()
                        )
                    )
                    .ToArray()
            );
        }

        [Fact]
        public void Stage()
        {
            var stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            var chain = MakeChainWithStagePolicy(stagePolicy);

            stagePolicy.Stage(chain, _txs[_accounts[0].Address][0]);
            stagePolicy.Stage(chain, _txs[_accounts[0].Address][1]);
            stagePolicy.Stage(chain, _txs[_accounts[1].Address][0]);
            stagePolicy.Stage(chain, _txs[_accounts[2].Address][0]);

            AssertTxs(
                chain,
                stagePolicy,
                _txs[_accounts[0].Address][0],
                _txs[_accounts[0].Address][1],
                _txs[_accounts[1].Address][0],
                _txs[_accounts[2].Address][0]
            );
        }

        [Fact]
        public void StageOverQuota()
        {
            var stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            var chain = MakeChainWithStagePolicy(stagePolicy);

            stagePolicy.Stage(chain, _txs[_accounts[0].Address][0]);
            stagePolicy.Stage(chain, _txs[_accounts[0].Address][1]);
            stagePolicy.Stage(chain, _txs[_accounts[0].Address][2]);
            stagePolicy.Stage(chain, _txs[_accounts[0].Address][3]);

            AssertTxs(
                chain,
                stagePolicy,
                _txs[_accounts[0].Address][0],
                _txs[_accounts[0].Address][1]
            );
        }

        [Fact]
        public void StageOverQuotaInverseOrder()
        {
            var stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            var chain = MakeChainWithStagePolicy(stagePolicy);

            stagePolicy.Stage(chain, _txs[_accounts[0].Address][3]);
            stagePolicy.Stage(chain, _txs[_accounts[0].Address][2]);
            stagePolicy.Stage(chain, _txs[_accounts[0].Address][1]);
            stagePolicy.Stage(chain, _txs[_accounts[0].Address][0]);

            AssertTxs(
                chain,
                stagePolicy,
                _txs[_accounts[0].Address][0],
                _txs[_accounts[0].Address][1]
            );
        }

        [Fact]
        public void StageOverQuotaOutOfOrder()
        {
            var stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            var chain = MakeChainWithStagePolicy(stagePolicy);

            stagePolicy.Stage(chain, _txs[_accounts[0].Address][2]);
            stagePolicy.Stage(chain, _txs[_accounts[0].Address][1]);
            stagePolicy.Stage(chain, _txs[_accounts[0].Address][3]);
            stagePolicy.Stage(chain, _txs[_accounts[0].Address][0]);

            AssertTxs(
                chain,
                stagePolicy,
                _txs[_accounts[0].Address][0],
                _txs[_accounts[0].Address][1]
            );
        }

        [Fact]
        public void StageSameNonce()
        {
            var stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            var chain = MakeChainWithStagePolicy(stagePolicy);
            var txA = Transaction.Create(0, _accounts[0], default, new ActionBase[0].ToPlainValues());
            var txB = Transaction.Create(0, _accounts[0], default, new ActionBase[0].ToPlainValues());
            var txC = Transaction.Create(0, _accounts[0], default, new ActionBase[0].ToPlainValues());

            stagePolicy.Stage(chain, txA);
            stagePolicy.Stage(chain, txB);
            stagePolicy.Stage(chain, txC);

            AssertTxs(chain, stagePolicy, txA, txB);
        }

        [Fact]
        public async Task StateFromMultiThread()
        {
            var stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            var chain = MakeChainWithStagePolicy(stagePolicy);

            await Task.WhenAll(
                Enumerable
                    .Range(0, 40)
                    .Select(i => Task.Run(() => { stagePolicy.Stage(chain, _txs[_accounts[i / 10].Address][i % 10]); }))
            );
            AssertTxs(
                chain,
                stagePolicy,
                _txs[_accounts[0].Address][0],
                _txs[_accounts[0].Address][1],
                _txs[_accounts[1].Address][0],
                _txs[_accounts[1].Address][1],
                _txs[_accounts[2].Address][0],
                _txs[_accounts[2].Address][1],
                _txs[_accounts[3].Address][0],
                _txs[_accounts[3].Address][1]
            );
        }

        [Fact]
        public void IterateAfterUnstage()
        {
            var stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            var chain = MakeChainWithStagePolicy(stagePolicy);

            stagePolicy.Stage(chain, _txs[_accounts[0].Address][0]);
            stagePolicy.Stage(chain, _txs[_accounts[0].Address][1]);
            stagePolicy.Stage(chain, _txs[_accounts[0].Address][2]);
            stagePolicy.Stage(chain, _txs[_accounts[0].Address][3]);

            AssertTxs(
                chain,
                stagePolicy,
                _txs[_accounts[0].Address][0],
                _txs[_accounts[0].Address][1]
            );

            stagePolicy.Unstage(chain, _txs[_accounts[0].Address][0].Id);

            AssertTxs(
                chain,
                stagePolicy,
                _txs[_accounts[0].Address][1],
                _txs[_accounts[0].Address][2]
            );
        }

        [Fact]
        public void CalculateNextTxNonceCorrectWhenTxOverQuota()
        {
            var stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            var chain = MakeChainWithStagePolicy(stagePolicy);

            var nextTxNonce = chain.GetNextTxNonce(_accounts[0].Address);
            Assert.Equal(0, nextTxNonce);
            var txA = Transaction.Create(nextTxNonce, _accounts[0], default, new ActionBase[0].ToPlainValues());
            stagePolicy.Stage(chain, txA);

            nextTxNonce = chain.GetNextTxNonce(_accounts[0].Address);
            Assert.Equal(1, nextTxNonce);
            var txB = Transaction.Create(nextTxNonce, _accounts[0], default, new ActionBase[0].ToPlainValues());
            stagePolicy.Stage(chain, txB);

            nextTxNonce = chain.GetNextTxNonce(_accounts[0].Address);
            Assert.Equal(2, nextTxNonce);
            var txC = Transaction.Create(nextTxNonce, _accounts[0], default, new ActionBase[0].ToPlainValues());
            stagePolicy.Stage(chain, txC);

            nextTxNonce = chain.GetNextTxNonce(_accounts[0].Address);
            Assert.Equal(3, nextTxNonce);

            AssertTxs(
                chain,
                stagePolicy,
                txA,
                txB);
        }

        private void AssertTxs(BlockChain blockChain, NCStagePolicy policy, params Transaction[] txs)
        {
            foreach (var tx in txs)
            {
                Assert.Equal(tx, policy.Get(blockChain, tx.Id, true));
            }

            Assert.Equal(
                txs.ToHashSet(),
                policy.Iterate(blockChain, true).ToHashSet()
            );
        }

        private BlockChain MakeChainWithStagePolicy(NCStagePolicy stagePolicy)
        {
            var blockPolicySource = new BlockPolicySource();
            var policy = blockPolicySource.GetPolicy();
            var chain =
                BlockChainHelper.MakeBlockChain(
                    new[] { new BlockRenderer(), },
                    policy,
                    stagePolicy);
            return chain;
        }
    }
}
