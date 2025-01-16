namespace Lib9c.Tests.Delegation
{
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Xunit;

    public class DelegatorTest
    {
        private DelegationFixture _fixture;

        public DelegatorTest()
        {
            _fixture = new DelegationFixture();
        }

        [Fact]
        public void Ctor()
        {
            var address = new Address("0x070e5719767CfB86712C31F5AB0072c48959d862");
            var delegator = new TestDelegator(address, _fixture.TestRepository.DelegatorAccountAddress, _fixture.TestRepository);
            Assert.Equal(address, delegator.Address);
            Assert.Empty(delegator.Delegatees);
        }

        [Fact]
        public void GetSet()
        {
            var repo = _fixture.TestRepository;
            var delegator = _fixture.TestDelegator1;
            var delegatee = _fixture.TestDelegatee1;
            repo.MintAsset(delegator.Address, delegatee.DelegationCurrency * 100);
            delegator.Delegate(delegatee, delegatee.DelegationCurrency * 10, 10L);
            var delegatorRecon = repo.GetDelegator(delegator.Address);
            Assert.Equal(delegator.Address, delegatorRecon.Address);
            Assert.Equal(delegatee.Address, Assert.Single(delegatorRecon.Delegatees));
        }

        [Fact]
        public void Delegate()
        {
            var repo = _fixture.TestRepository;
            var delegator = _fixture.TestDelegator1;
            var delegatee1 = _fixture.TestDelegatee1;
            var delegatee2 = _fixture.TestDelegatee2;
            var delegatorInitialBalance = delegatee1.DelegationCurrency * 100;
            repo.MintAsset(delegator.Address, delegatorInitialBalance);

            var delegateFAV = delegatee1.DelegationCurrency * 10;
            var delegateShare = delegatee1.ShareFromFAV(delegateFAV);
            delegator.Delegate(delegatee1, delegateFAV, 1L);
            var delegatorBalance = repo.World.GetBalance(delegator.Address, delegatee1.DelegationCurrency);
            var delegateeBalance = repo.World.GetBalance(delegatee1.DelegationPoolAddress, delegatee1.DelegationCurrency);
            var share = repo.GetBond(delegatee1, delegator.Address).Share;
            Assert.Equal(delegatorInitialBalance - delegateFAV, delegatorBalance);
            Assert.Equal(delegateFAV, delegateeBalance);
            Assert.Equal(delegateShare, share);
            Assert.Equal(delegateFAV, delegatee1.TotalDelegated);
            Assert.Equal(delegateShare, delegatee1.TotalShares);
            Assert.Equal(delegatee1.Address, Assert.Single(delegator.Delegatees));

            var delegateFAV2 = delegatee1.DelegationCurrency * 20;
            var delegateShare2 = delegatee1.ShareFromFAV(delegateFAV2);
            delegator.Delegate(delegatee1, delegateFAV2, 2L);
            delegatorBalance = repo.World.GetBalance(delegator.Address, delegatee1.DelegationCurrency);
            delegateeBalance = repo.World.GetBalance(delegatee1.DelegationPoolAddress, delegatee1.DelegationCurrency);
            share = repo.GetBond(delegatee1, delegator.Address).Share;
            Assert.Equal(delegatorInitialBalance - delegateFAV - delegateFAV2, delegatorBalance);
            Assert.Equal(delegateFAV + delegateFAV2, delegateeBalance);
            Assert.Equal(delegateShare + delegateShare2, share);
            Assert.Equal(delegateFAV + delegateFAV2, delegatee1.TotalDelegated);
            Assert.Equal(delegateShare + delegateShare2, delegatee1.TotalShares);
            Assert.Equal(delegatee1.Address, Assert.Single(delegator.Delegatees));

            delegator.Delegate(delegatee2, delegateFAV, 3L);
            delegatorBalance = repo.World.GetBalance(delegator.Address, delegatee2.DelegationCurrency);
            delegateeBalance = repo.World.GetBalance(delegatee2.DelegationPoolAddress, delegatee2.DelegationCurrency);
            share = repo.GetBond(delegatee2, delegator.Address).Share;
            Assert.Equal(delegatorInitialBalance - delegateFAV * 2 - delegateFAV2, delegatorBalance);
            Assert.Equal(delegateFAV, delegateeBalance);
            Assert.Equal(delegateShare, share);
            Assert.Equal(delegateFAV, delegatee2.TotalDelegated);
            Assert.Equal(delegateShare, delegatee2.TotalShares);
            Assert.Equal(2, delegator.Delegatees.Count);
            Assert.Contains(delegatee1.Address, delegator.Delegatees);
            Assert.Contains(delegatee2.Address, delegator.Delegatees);
        }

        [Fact]
        public void Undelegate()
        {
            var repo = _fixture.TestRepository;
            var delegator = _fixture.TestDelegator1;
            var delegatee = _fixture.TestDelegatee1;
            var delegatorInitialBalance = delegatee.DelegationCurrency * 100;
            repo.MintAsset(delegator.Address, delegatorInitialBalance);
            var delegatingFAV = delegatee.DelegationCurrency * 10;
            delegator.Delegate(delegatee, delegatingFAV, 9L);
            var initialShare = repo.GetBond(delegatee, delegator.Address).Share;
            var undelegatingShare = initialShare / 3;
            var undelegatingFAV = delegatee.FAVFromShare(undelegatingShare);
            delegator.Undelegate(delegatee, undelegatingShare, 10L);
            var delegatorBalance = repo.World.GetBalance(delegator.Address, delegatee.DelegationCurrency);
            var delegateeBalance = repo.World.GetBalance(delegatee.DelegationPoolAddress, delegatee.DelegationCurrency);
            var share1 = repo.GetBond(delegatee, delegator.Address).Share;
            var unbondLockIn = repo.GetUnbondLockIn(delegatee, delegator.Address);
            Assert.Equal(delegatorInitialBalance - delegatingFAV, delegatorBalance);
            Assert.Equal(delegatingFAV, delegateeBalance);
            Assert.Equal(initialShare - undelegatingShare, share1);
            Assert.Equal(delegatingFAV - undelegatingFAV, delegatee.TotalDelegated);
            Assert.Equal(initialShare - undelegatingShare, delegatee.TotalShares);
            Assert.Equal(delegatee.Address, Assert.Single(delegator.Delegatees));
            var entriesByExpireHeight = Assert.Single(unbondLockIn.Entries);
            Assert.Equal(10L + delegatee.UnbondingPeriod, entriesByExpireHeight.Key);
            var entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(undelegatingFAV, entry.InitialUnbondingFAV);
            Assert.Equal(undelegatingFAV, entry.UnbondingFAV);
            Assert.Equal(10L, entry.CreationHeight);
            Assert.Equal(10L + delegatee.UnbondingPeriod, entry.ExpireHeight);

            undelegatingShare = repo.GetBond(delegatee, delegator.Address).Share;
            var undelegatingFAV2 = delegatee.FAVFromShare(undelegatingShare);
            delegator.Undelegate(delegatee, undelegatingShare, 12L);
            delegatorBalance = repo.World.GetBalance(delegator.Address, delegatee.DelegationCurrency);
            delegateeBalance = repo.World.GetBalance(delegatee.DelegationPoolAddress, delegatee.DelegationCurrency);
            var share2 = repo.GetBond(delegatee, delegator.Address).Share;
            unbondLockIn = repo.GetUnbondLockIn(delegatee, delegator.Address);
            Assert.Equal(delegatorInitialBalance - delegatingFAV, delegatorBalance);
            Assert.Equal(delegatingFAV, delegateeBalance);
            Assert.Equal(share1 - undelegatingShare, share2);
            Assert.Equal(delegatee.DelegationCurrency * 0, delegatee.TotalDelegated);
            Assert.Equal(System.Numerics.BigInteger.Zero, delegatee.TotalShares);
            Assert.Empty(delegator.Delegatees);
            Assert.Equal(2, unbondLockIn.Entries.Count);

            unbondLockIn = unbondLockIn.Release(10L + delegatee.UnbondingPeriod - 1, out _);
            delegatorBalance = repo.World.GetBalance(delegator.Address, delegatee.DelegationCurrency);
            delegateeBalance = repo.World.GetBalance(delegatee.DelegationPoolAddress, delegatee.DelegationCurrency);
            Assert.Equal(2, unbondLockIn.Entries.Count);
            entriesByExpireHeight = unbondLockIn.Entries.ElementAt(0);
            Assert.Equal(10L + delegatee.UnbondingPeriod, entriesByExpireHeight.Key);
            entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(undelegatingFAV, entry.InitialUnbondingFAV);
            Assert.Equal(undelegatingFAV, entry.UnbondingFAV);
            Assert.Equal(10L, entry.CreationHeight);
            Assert.Equal(10L + delegatee.UnbondingPeriod, entry.ExpireHeight);
            entriesByExpireHeight = unbondLockIn.Entries.ElementAt(1);
            Assert.Equal(12L + delegatee.UnbondingPeriod, entriesByExpireHeight.Key);
            entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(undelegatingFAV2, entry.InitialUnbondingFAV);
            Assert.Equal(undelegatingFAV2, entry.UnbondingFAV);
            Assert.Equal(12L, entry.CreationHeight);
            Assert.Equal(12L + delegatee.UnbondingPeriod, entry.ExpireHeight);
            Assert.Equal(delegatorInitialBalance - delegatingFAV, delegatorBalance);
            Assert.Equal(delegatingFAV, delegateeBalance);

            unbondLockIn = unbondLockIn.Release(10L + delegatee.UnbondingPeriod, out _);
            delegatorBalance = repo.World.GetBalance(delegator.Address, delegatee.DelegationCurrency);
            delegateeBalance = repo.World.GetBalance(delegatee.DelegationPoolAddress, delegatee.DelegationCurrency);
            entriesByExpireHeight = Assert.Single(unbondLockIn.Entries);
            Assert.Equal(12L + delegatee.UnbondingPeriod, entriesByExpireHeight.Key);
            entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(undelegatingFAV2, entry.InitialUnbondingFAV);
            Assert.Equal(undelegatingFAV2, entry.UnbondingFAV);
            Assert.Equal(12L, entry.CreationHeight);
            Assert.Equal(12L + delegatee.UnbondingPeriod, entry.ExpireHeight);
            Assert.Equal(delegatorInitialBalance - delegatingFAV + undelegatingFAV, delegatorBalance);
            Assert.Equal(delegatingFAV - undelegatingFAV, delegateeBalance);

            unbondLockIn = unbondLockIn.Release(12L + delegatee.UnbondingPeriod, out _);
            delegatorBalance = repo.World.GetBalance(delegator.Address, delegatee.DelegationCurrency);
            delegateeBalance = repo.World.GetBalance(delegatee.DelegationPoolAddress, delegatee.DelegationCurrency);
            Assert.Empty(unbondLockIn.Entries);
            Assert.Equal(delegatorInitialBalance, delegatorBalance);
            Assert.Equal(delegatee.DelegationCurrency * 0, delegateeBalance);
        }

        [Fact]
        public void Redelegate()
        {
            var repo = _fixture.TestRepository;
            var delegator = _fixture.TestDelegator1;
            var delegatee1 = _fixture.TestDelegatee1;
            var delegatee2 = _fixture.TestDelegatee2;
            var delegatorInitialBalance = delegatee1.DelegationCurrency * 100;
            repo.MintAsset(delegator.Address, delegatorInitialBalance);
            var delegatingFAV = delegatee1.DelegationCurrency * 10;
            delegator.Delegate(delegatee1, delegatingFAV, 1L);
            Assert.Equal(delegatee1.Address, Assert.Single(delegator.Delegatees));
            var initialShare = repo.GetBond(delegatee1, delegator.Address).Share;
            var redelegatingShare = initialShare / 3;
            var redelegatingFAV = delegatee1.FAVFromShare(redelegatingShare);
            var redelegatedDstShare = delegatee2.ShareFromFAV(redelegatingFAV);
            delegator.Redelegate(delegatee1, delegatee2, redelegatingShare, 10L);
            var delegatorBalance = repo.World.GetBalance(delegator.Address, delegatee1.DelegationCurrency);
            var delegatee1Balance = repo.World.GetBalance(delegatee1.DelegationPoolAddress, delegatee1.DelegationCurrency);
            var delegatee2Balance = repo.World.GetBalance(delegatee2.DelegationPoolAddress, delegatee2.DelegationCurrency);
            var share1 = repo.GetBond(delegatee1, delegator.Address).Share;
            var share2 = repo.GetBond(delegatee2, delegator.Address).Share;
            var rebondGrace = repo.GetRebondGrace(delegatee1, delegator.Address);
            Assert.Equal(delegatorInitialBalance - delegatingFAV, delegatorBalance);
            Assert.Equal(delegatingFAV - redelegatingFAV, delegatee1Balance);
            Assert.Equal(initialShare - redelegatingShare, share1);
            Assert.Equal(initialShare - redelegatingShare, delegatee1.TotalShares);
            Assert.Equal(redelegatedDstShare, share2);
            Assert.Equal(redelegatedDstShare, delegatee2.TotalShares);
            Assert.Equal(delegatingFAV - redelegatingFAV, delegatee1.TotalDelegated);
            Assert.Equal(redelegatingFAV, delegatee2.TotalDelegated);
            Assert.Equal(2, delegator.Delegatees.Count);
            var entriesByExpireHeight = Assert.Single(rebondGrace.Entries);
            Assert.Equal(10L + delegatee1.UnbondingPeriod, entriesByExpireHeight.Key);
            var entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(delegatee2.Address, entry.UnbondeeAddress);
            Assert.Equal(redelegatingFAV, entry.InitialUnbondingFAV);
            Assert.Equal(redelegatingFAV, entry.UnbondingFAV);
            Assert.Equal(10L, entry.CreationHeight);
            Assert.Equal(10L + delegatee1.UnbondingPeriod, entry.ExpireHeight);

            var redelegatingShare2 = repo.GetBond(delegatee1, delegator.Address).Share;
            var redelegatingFAV2 = delegatee1.FAVFromShare(redelegatingShare2);
            var redelegatedDstShare2 = delegatee2.ShareFromFAV(redelegatingFAV2);
            delegator.Redelegate(delegatee1, delegatee2, redelegatingShare2, 12L);
            delegatorBalance = repo.World.GetBalance(delegator.Address, delegatee1.DelegationCurrency);
            delegatee1Balance = repo.World.GetBalance(delegatee1.DelegationPoolAddress, delegatee1.DelegationCurrency);
            delegatee2Balance = repo.World.GetBalance(delegatee2.DelegationPoolAddress, delegatee2.DelegationCurrency);
            share1 = repo.GetBond(delegatee1, delegator.Address).Share;
            share2 = repo.GetBond(delegatee2, delegator.Address).Share;
            rebondGrace = repo.GetRebondGrace(delegatee1, delegator.Address);
            Assert.Equal(delegatorInitialBalance - delegatingFAV, delegatorBalance);
            Assert.Equal(delegatingFAV - redelegatingFAV - redelegatingFAV2, delegatee1Balance);
            Assert.Equal(initialShare - redelegatingShare - redelegatingShare2, share1);
            Assert.Equal(initialShare - redelegatingShare - redelegatingShare2, delegatee1.TotalShares);
            Assert.Equal(redelegatedDstShare + redelegatedDstShare2, share2);
            Assert.Equal(redelegatedDstShare + redelegatedDstShare2, delegatee2.TotalShares);
            Assert.Equal(delegatingFAV - redelegatingFAV - redelegatingFAV2, delegatee1.TotalDelegated);
            Assert.Equal(redelegatingFAV + redelegatingFAV2, delegatee2.TotalDelegated);
            Assert.Equal(delegatee2.Address, Assert.Single(delegator.Delegatees));
            Assert.Equal(2, rebondGrace.Entries.Count);

            rebondGrace = rebondGrace.Release(10L + delegatee1.UnbondingPeriod - 1, out _);
            Assert.Equal(2, rebondGrace.Entries.Count);
            entriesByExpireHeight = rebondGrace.Entries.ElementAt(0);
            Assert.Equal(10L + delegatee1.UnbondingPeriod, entriesByExpireHeight.Key);
            entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(delegatee2.Address, entry.UnbondeeAddress);
            Assert.Equal(redelegatingFAV, entry.InitialUnbondingFAV);
            Assert.Equal(redelegatingFAV, entry.UnbondingFAV);
            Assert.Equal(10L, entry.CreationHeight);
            Assert.Equal(10L + delegatee1.UnbondingPeriod, entry.ExpireHeight);
            entriesByExpireHeight = rebondGrace.Entries.ElementAt(1);
            Assert.Equal(12L + delegatee1.UnbondingPeriod, entriesByExpireHeight.Key);
            entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(delegatee2.Address, entry.UnbondeeAddress);
            Assert.Equal(redelegatingFAV2, entry.InitialUnbondingFAV);
            Assert.Equal(redelegatingFAV2, entry.UnbondingFAV);
            Assert.Equal(12L, entry.CreationHeight);
            Assert.Equal(12L + delegatee1.UnbondingPeriod, entry.ExpireHeight);

            rebondGrace = rebondGrace.Release(10L + delegatee1.UnbondingPeriod, out _);
            entriesByExpireHeight = Assert.Single(rebondGrace.Entries);
            Assert.Equal(12L + delegatee1.UnbondingPeriod, entriesByExpireHeight.Key);
            entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(delegatee2.Address, entry.UnbondeeAddress);
            Assert.Equal(redelegatingFAV2, entry.InitialUnbondingFAV);
            Assert.Equal(redelegatingFAV2, entry.UnbondingFAV);
            Assert.Equal(12L, entry.CreationHeight);
            Assert.Equal(12L + delegatee1.UnbondingPeriod, entry.ExpireHeight);

            rebondGrace = rebondGrace.Release(12L + delegatee1.UnbondingPeriod, out _);
            Assert.Empty(rebondGrace.Entries);
        }

        [Fact]
        public void RewardOnDelegate()
        {
            var repo = _fixture.TestRepository;
            var delegator1 = _fixture.TestDelegator1;
            var delegator2 = _fixture.TestDelegator2;
            var delegatee = _fixture.TestDelegatee1;
            var delegatorInitialBalance = delegatee.DelegationCurrency * 100;
            repo.MintAsset(delegator1.Address, delegatorInitialBalance);
            repo.MintAsset(delegator2.Address, delegatorInitialBalance);

            var rewards = delegatee.RewardCurrencies.Select(r => r * 100);

            var delegatingFAV1 = delegatee.DelegationCurrency * 10;
            delegator1.Delegate(delegatee, delegatingFAV1, 10L);
            var delegator1Balance = repo.World.GetBalance(delegator1.Address, delegatee.DelegationCurrency);
            var delegateeBalance = repo.World.GetBalance(delegatee.DelegationPoolAddress, delegatee.DelegationCurrency);
            var share1 = repo.GetBond(delegatee, delegator1.Address).Share;
            Assert.Equal(delegatorInitialBalance - delegatingFAV1, delegator1Balance);

            var delegatingFAV2 = delegatee.DelegationCurrency * 20;
            delegator2.Delegate(delegatee, delegatingFAV2, 10L);
            var delegator2Balance = repo.World.GetBalance(delegator2.Address, delegatee.DelegationCurrency);
            delegateeBalance = repo.World.GetBalance(delegatee.DelegationPoolAddress, delegatee.DelegationCurrency);
            var share2 = repo.GetBond(delegatee, delegator2.Address).Share;
            Assert.Equal(delegatorInitialBalance - delegatingFAV2, delegator2Balance);

            var totalShares = delegatee.TotalShares;

            foreach (var reward in rewards)
            {
                repo.MintAsset(delegatee.RewardPoolAddress, reward);
            }

            // EndBlock after delegatee's reward
            delegatee.CollectRewards(10L);

            delegatingFAV1 = delegatee.DelegationCurrency * 10;
            delegator1.Delegate(delegatee, delegatingFAV1, 11L);
            delegator1Balance = repo.World.GetBalance(delegator1.Address, delegatee.DelegationCurrency);
            delegator2Balance = repo.World.GetBalance(delegator2.Address, delegatee.DelegationCurrency);
            var delegator1RewardBalances = delegatee.RewardCurrencies.Select(
                c => repo.World.GetBalance(delegator1.Address, c));
            var collectedRewards = delegatee.RewardCurrencies.Select(
               c => repo.World.GetBalance(delegatee.DistributionPoolAddress(), c));

            var rewards1 = rewards.Select(r => (r * share1).DivRem(totalShares, out _));
            Assert.Equal(delegatorInitialBalance - delegatingFAV1 * 2, delegator1Balance);
            Assert.Equal(delegatorInitialBalance - delegatingFAV2, delegator2Balance);
            Assert.Equal(rewards1, delegator1RewardBalances);
            Assert.Equal(rewards.Zip(rewards1, (f, s) => f - s), collectedRewards);

            delegator2.Delegate(delegatee, delegatingFAV2, 11L);
            delegator2Balance = repo.World.GetBalance(delegator2.Address, delegatee.DelegationCurrency);
            delegator1RewardBalances = delegatee.RewardCurrencies.Select(
                c => repo.World.GetBalance(delegator1.Address, c));
            var delegator2RewardBalances = delegatee.RewardCurrencies.Select(
                c => repo.World.GetBalance(delegator2.Address, c));
            collectedRewards = delegatee.RewardCurrencies.Select(
               c => repo.World.GetBalance(delegatee.DistributionPoolAddress(), c));

            var rewards2 = rewards.Select(r => (r * share2).DivRem(totalShares, out _));
            Assert.Equal(delegatorInitialBalance - delegatingFAV1 * 2, delegator1Balance);
            Assert.Equal(delegatorInitialBalance - delegatingFAV2 * 2, delegator2Balance);
            Assert.Equal(rewards1, delegator1RewardBalances);
            Assert.Equal(rewards2, delegator2RewardBalances);

            // Flushing to remainder pool is now inactive.
            // Assert.Equal(delegatee.RewardCurrencies.Select(c => c * 0), collectedRewards);
            Assert.Equal(
                rewards.Zip(rewards1.Zip(rewards2, (f, s) => f + s), (f, s) => f - s).ToArray(),
                collectedRewards);
        }

        [Fact]
        public void RewardOnUndelegate()
        {
            var repo = _fixture.TestRepository;
            var delegator1 = _fixture.TestDelegator1;
            var delegator2 = _fixture.TestDelegator2;
            var delegatee = _fixture.TestDelegatee1;
            var delegatorInitialBalance = delegatee.DelegationCurrency * 100;
            repo.MintAsset(delegator1.Address, delegatorInitialBalance);
            repo.MintAsset(delegator2.Address, delegatorInitialBalance);

            var rewards = delegatee.RewardCurrencies.Select(r => r * 100);

            var delegatingFAV1 = delegatee.DelegationCurrency * 10;
            delegator1.Delegate(delegatee, delegatingFAV1, 10L);
            var delegator1Balance = repo.World.GetBalance(delegator1.Address, delegatee.DelegationCurrency);
            var delegateeBalance = repo.World.GetBalance(delegatee.DelegationPoolAddress, delegatee.DelegationCurrency);
            var share1 = repo.GetBond(delegatee, delegator1.Address).Share;
            Assert.Equal(delegatorInitialBalance - delegatingFAV1, delegator1Balance);

            var delegatingFAV2 = delegatee.DelegationCurrency * 20;
            delegator2.Delegate(delegatee, delegatingFAV2, 10L);
            var delegator2Balance = repo.World.GetBalance(delegator2.Address, delegatee.DelegationCurrency);
            delegateeBalance = repo.World.GetBalance(delegatee.DelegationPoolAddress, delegatee.DelegationCurrency);
            var share2 = repo.GetBond(delegatee, delegator2.Address).Share;
            Assert.Equal(delegatorInitialBalance - delegatingFAV2, delegator2Balance);

            var totalShares = delegatee.TotalShares;

            foreach (var reward in rewards)
            {
                repo.MintAsset(delegatee.RewardPoolAddress, reward);
            }

            // BeginBlock after delegatee's reward
            delegatee.CollectRewards(11L);

            var shareToUndelegate = repo.GetBond(delegatee, delegator1.Address).Share / 3;
            delegator1.Undelegate(delegatee, shareToUndelegate, 11L);
            delegator1Balance = repo.World.GetBalance(delegator1.Address, delegatee.DelegationCurrency);
            var delegator1RewardBalances = delegatee.RewardCurrencies.Select(
                c => repo.World.GetBalance(delegator1.Address, c));
            var collectedRewards = delegatee.RewardCurrencies.Select(
                c => repo.World.GetBalance(delegatee.DistributionPoolAddress(), c));

            var rewards1 = rewards.Select(r => (r * share1).DivRem(totalShares, out _));
            Assert.Equal(delegatorInitialBalance - delegatingFAV1, delegator1Balance);
            Assert.Equal(delegatorInitialBalance - delegatingFAV2, delegator2Balance);
            Assert.Equal(rewards1, delegator1RewardBalances);
            Assert.Equal(rewards.Zip(rewards1, (f, s) => f - s), collectedRewards);

            shareToUndelegate = repo.GetBond(delegatee, delegator2.Address).Share / 2;
            delegator2.Undelegate(delegatee, shareToUndelegate, 11L);
            delegator2Balance = repo.World.GetBalance(delegator2.Address, delegatee.DelegationCurrency);
            delegator1RewardBalances = delegatee.RewardCurrencies.Select(
                c => repo.World.GetBalance(delegator1.Address, c));
            var delegator2RewardBalances = delegatee.RewardCurrencies.Select(
                c => repo.World.GetBalance(delegator2.Address, c));
            collectedRewards = delegatee.RewardCurrencies.Select(
                c => repo.World.GetBalance(delegatee.DistributionPoolAddress(), c));

            var rewards2 = rewards.Select(r => (r * share2).DivRem(totalShares, out _));
            Assert.Equal(delegatorInitialBalance - delegatingFAV1, delegator1Balance);
            Assert.Equal(delegatorInitialBalance - delegatingFAV2, delegator2Balance);
            Assert.Equal(rewards1, delegator1RewardBalances);
            Assert.Equal(rewards2, delegator2RewardBalances);

            // Flushing to remainder pool is now inactive.
            Assert.Equal(
                rewards.Zip(rewards1.Zip(rewards2, (f, s) => f + s), (f, s) => f - s).ToArray(),
                collectedRewards);
        }

        [Fact]
        public void RewardOnRedelegate()
        {
            var repo = _fixture.TestRepository;
            var delegator1 = _fixture.TestDelegator1;
            var delegator2 = _fixture.TestDelegator2;
            var delegatee = _fixture.TestDelegatee1;
            var dstDelegatee = _fixture.TestDelegatee2;
            var delegatorInitialBalance = delegatee.DelegationCurrency * 100;
            repo.MintAsset(delegator1.Address, delegatorInitialBalance);
            repo.MintAsset(delegator2.Address, delegatorInitialBalance);

            var rewards = delegatee.RewardCurrencies.Select(r => r * 100);

            var delegatingFAV1 = delegatee.DelegationCurrency * 10;
            delegator1.Delegate(delegatee, delegatingFAV1, 10L);
            var delegator1Balance = repo.World.GetBalance(delegator1.Address, delegatee.DelegationCurrency);
            var delegateeBalance = repo.World.GetBalance(delegatee.DelegationPoolAddress, delegatee.DelegationCurrency);
            var share1 = repo.GetBond(delegatee, delegator1.Address).Share;
            Assert.Equal(delegatorInitialBalance - delegatingFAV1, delegator1Balance);

            var delegatingFAV2 = delegatee.DelegationCurrency * 20;
            delegator2.Delegate(delegatee, delegatingFAV2, 10L);
            var delegator2Balance = repo.World.GetBalance(delegator2.Address, delegatee.DelegationCurrency);
            delegateeBalance = repo.World.GetBalance(delegatee.DelegationPoolAddress, delegatee.DelegationCurrency);
            var share2 = repo.GetBond(delegatee, delegator2.Address).Share;
            Assert.Equal(delegatorInitialBalance - delegatingFAV2, delegator2Balance);

            var totalShares = delegatee.TotalShares;

            foreach (var reward in rewards)
            {
                repo.MintAsset(delegatee.RewardPoolAddress, reward);
            }

            // EndBlock after delegatee's reward
            delegatee.CollectRewards(10L);

            var shareToRedelegate = repo.GetBond(delegatee, delegator1.Address).Share / 3;
            delegator1.Redelegate(delegatee, dstDelegatee, shareToRedelegate, 11L);
            delegator1Balance = repo.World.GetBalance(delegator1.Address, delegatee.DelegationCurrency);
            var delegator1RewardBalances = delegatee.RewardCurrencies.Select(
               c => repo.World.GetBalance(delegator1.Address, c));
            var collectedRewards = delegatee.RewardCurrencies.Select(
               c => repo.World.GetBalance(delegatee.DistributionPoolAddress(), c));

            var rewards1 = rewards.Select(r => (r * share1).DivRem(totalShares, out _));
            Assert.Equal(delegatorInitialBalance - delegatingFAV1, delegator1Balance);
            Assert.Equal(delegatorInitialBalance - delegatingFAV2, delegator2Balance);
            Assert.Equal(rewards1, delegator1RewardBalances);
            Assert.Equal(rewards.Zip(rewards1, (f, s) => f - s), collectedRewards);

            shareToRedelegate = repo.GetBond(delegatee, delegator2.Address).Share / 2;
            delegator2.Redelegate(delegatee, dstDelegatee, shareToRedelegate, 11L);
            delegator2Balance = repo.World.GetBalance(delegator2.Address, delegatee.DelegationCurrency);
            delegator1RewardBalances = delegatee.RewardCurrencies.Select(
                c => repo.World.GetBalance(delegator1.Address, c));
            var delegator2RewardBalances = delegatee.RewardCurrencies.Select(
                c => repo.World.GetBalance(delegator2.Address, c));
            collectedRewards = delegatee.RewardCurrencies.Select(
               c => repo.World.GetBalance(delegatee.DistributionPoolAddress(), c));

            var rewards2 = rewards.Select(r => (r * share2).DivRem(totalShares, out _));
            Assert.Equal(delegatorInitialBalance - delegatingFAV1, delegator1Balance);
            Assert.Equal(delegatorInitialBalance - delegatingFAV2, delegator2Balance);
            Assert.Equal(rewards1, delegator1RewardBalances);
            Assert.Equal(rewards2, delegator2RewardBalances);

            // Flushing to remainder pool is now inactive.
            // Assert.Equal(delegatee.RewardCurrencies.Select(c => c * 0), collectedRewards);
            Assert.Equal(
                rewards.Zip(rewards1.Zip(rewards2, (f, s) => f + s), (f, s) => f - s).ToArray(),
                collectedRewards);
        }

        [Fact]
        public void RewardOnClaim()
        {
            var repo = _fixture.TestRepository;
            var delegator1 = _fixture.TestDelegator1;
            var delegator2 = _fixture.TestDelegator2;
            var delegatee = _fixture.TestDelegatee1;
            var dstDelegatee = _fixture.TestDelegatee2;
            var delegatorInitialBalance = delegatee.DelegationCurrency * 100;
            repo.MintAsset(delegator1.Address, delegatorInitialBalance);
            repo.MintAsset(delegator2.Address, delegatorInitialBalance);

            var rewards = delegatee.RewardCurrencies.Select(r => r * 100);

            var delegatingFAV1 = delegatee.DelegationCurrency * 10;
            delegator1.Delegate(delegatee, delegatingFAV1, 10L);
            var delegator1Balance = repo.World.GetBalance(delegator1.Address, delegatee.DelegationCurrency);
            var delegateeBalance = repo.World.GetBalance(delegatee.DelegationPoolAddress, delegatee.DelegationCurrency);
            var share1 = repo.GetBond(delegatee, delegator1.Address).Share;
            Assert.Equal(delegatorInitialBalance - delegatingFAV1, delegator1Balance);

            var delegatingFAV2 = delegatee.DelegationCurrency * 20;
            delegator2.Delegate(delegatee, delegatingFAV2, 10L);
            var delegator2Balance = repo.World.GetBalance(delegator2.Address, delegatee.DelegationCurrency);
            delegateeBalance = repo.World.GetBalance(delegatee.DelegationPoolAddress, delegatee.DelegationCurrency);
            var share2 = repo.GetBond(delegatee, delegator2.Address).Share;
            Assert.Equal(delegatorInitialBalance - delegatingFAV2, delegator2Balance);

            var totalShares = delegatee.TotalShares;

            foreach (var reward in rewards)
            {
                repo.MintAsset(delegatee.RewardPoolAddress, reward);
            }

            // EndBlock after delegatee's reward
            delegatee.CollectRewards(10L);

            var shareToRedelegate = repo.GetBond(delegatee, delegator1.Address).Share / 3;
            delegator1.ClaimReward(delegatee, 11L);
            delegator1Balance = repo.World.GetBalance(delegator1.Address, delegatee.DelegationCurrency);
            var delegator1RewardBalances = delegatee.RewardCurrencies.Select(
               c => repo.World.GetBalance(delegator1.Address, c));
            var collectedRewards = delegatee.RewardCurrencies.Select(
               c => repo.World.GetBalance(delegatee.DistributionPoolAddress(), c));

            var rewards1 = rewards.Select(r => (r * share1).DivRem(totalShares, out _));
            Assert.Equal(delegatorInitialBalance - delegatingFAV1, delegator1Balance);
            Assert.Equal(delegatorInitialBalance - delegatingFAV2, delegator2Balance);
            Assert.Equal(rewards1, delegator1RewardBalances);
            Assert.Equal(rewards.Zip(rewards1, (f, s) => f - s), collectedRewards);

            shareToRedelegate = repo.GetBond(delegatee, delegator2.Address).Share / 2;
            delegator2.ClaimReward(delegatee, 11L);
            delegator2Balance = repo.World.GetBalance(delegator2.Address, delegatee.DelegationCurrency);
            delegator1RewardBalances = delegatee.RewardCurrencies.Select(
                c => repo.World.GetBalance(delegator1.Address, c));
            var delegator2RewardBalances = delegatee.RewardCurrencies.Select(
                c => repo.World.GetBalance(delegator2.Address, c));
            collectedRewards = delegatee.RewardCurrencies.Select(
               c => repo.World.GetBalance(delegatee.DistributionPoolAddress(), c));

            var rewards2 = rewards.Select(r => (r * share2).DivRem(totalShares, out _));
            Assert.Equal(delegatorInitialBalance - delegatingFAV1, delegator1Balance);
            Assert.Equal(delegatorInitialBalance - delegatingFAV2, delegator2Balance);
            Assert.Equal(rewards1, delegator1RewardBalances);
            Assert.Equal(rewards2, delegator2RewardBalances);

            // Flushing to remainder pool is now inactive.
            // Assert.Equal(delegatee.RewardCurrencies.Select(c => c * 0), collectedRewards);
            Assert.Equal(
                rewards.Zip(rewards1.Zip(rewards2, (f, s) => f + s), (f, s) => f - s).ToArray(),
                collectedRewards);
        }
    }
}
