namespace Lib9c.Tests.Delegation
{
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
            var delegator = new TestDelegator(address);
            Assert.Equal(address, delegator.Address);
            Assert.Empty(delegator.Delegatees);
        }

        [Fact]
        public void CtorWithBencoded()
        {
            var delegator = _fixture.TestDelegator1;
            var delegatee = _fixture.TestDelegatee1;
            var delegation = _fixture.Delegation1To1;
            delegator.Delegate(delegatee, delegatee.Currency * 10, delegation);

            var delegatorRecon = new TestDelegator(delegator.Address, delegator.Bencoded);
            Assert.Equal(delegator.Address, delegatorRecon.Address);
            Assert.Equal(delegatee.Address, Assert.Single(delegatorRecon.Delegatees));
        }

        [Fact]
        public void Delegate()
        {
            var delegator = _fixture.TestDelegator1;
            var delegatee1 = _fixture.TestDelegatee1;
            var delegatee2 = _fixture.TestDelegatee2;
            var delegation1 = _fixture.Delegation1To1;
            var delegation2 = _fixture.Delegation1To2;
            var delegateFAV = delegatee1.Currency * 10;

            delegator.Delegate(delegatee1, delegateFAV, delegation1);
            Assert.Equal(delegateFAV, delegation1.IncompleteBond);
            Assert.Equal(delegatee1.Address, Assert.Single(delegator.Delegatees));
            delegation1.Complete();
            delegator.Delegate(delegatee2, delegateFAV, delegation2);
            Assert.Equal(delegateFAV, delegation2.IncompleteBond);
            Assert.Equal(2, delegator.Delegatees.Count);
            Assert.Contains(delegatee1.Address, delegator.Delegatees);
            Assert.Contains(delegatee2.Address, delegator.Delegatees);
            delegation2.Complete();
        }

        [Fact]
        public void Undelegate()
        {
            var delegator = _fixture.TestDelegator1;
            var delegatee = _fixture.TestDelegatee1;
            var delegation = _fixture.Delegation1To1;
            delegator.Delegate(delegatee, delegatee.Currency * 10, delegation);

            var undelegatingShare = delegation.Bond.Share / 2;
            var undelegatingFAV = delegatee.FAVToUnbond(undelegatingShare);
            delegator.Undelegate(delegatee, undelegatingShare, 10L, delegation);
            Assert.Null(delegation.IncompleteBond);
            Assert.Null(delegation.IncompleteUnbond);
            Assert.Equal(delegatee.Address, Assert.Single(delegator.Delegatees));
            var entriesByExpireHeight = Assert.Single(delegation.UnbondLockIn.Entries);
            Assert.Equal(10L + delegatee.UnbondingPeriod, entriesByExpireHeight.Key);
            var entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(undelegatingFAV, entry.InitialLockInFAV);
            Assert.Equal(undelegatingFAV, entry.LockInFAV);
            Assert.Equal(10L, entry.CreationHeight);
            Assert.Equal(10L + delegatee.UnbondingPeriod, entry.ExpireHeight);

            undelegatingShare = delegation.Bond.Share;
            delegator.Undelegate(delegatee, undelegatingShare, 12L, delegation);
            Assert.Null(delegation.IncompleteBond);
            Assert.Null(delegation.IncompleteUnbond);
            Assert.Empty(delegator.Delegatees);
            Assert.Equal(2, delegation.UnbondLockIn.Entries.Count);

            delegation.UnbondLockIn.Release(10L + delegatee.UnbondingPeriod - 1);
            Assert.Equal(2, delegation.UnbondLockIn.Entries.Count);

            delegation.UnbondLockIn.Release(10L + delegatee.UnbondingPeriod);
            entriesByExpireHeight = Assert.Single(delegation.UnbondLockIn.Entries);
            Assert.Equal(12L + delegatee.UnbondingPeriod, entriesByExpireHeight.Key);
            entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(undelegatingFAV, entry.InitialLockInFAV);
            Assert.Equal(undelegatingFAV, entry.LockInFAV);
            Assert.Equal(12L, entry.CreationHeight);
            Assert.Equal(12L + delegatee.UnbondingPeriod, entry.ExpireHeight);

            delegation.UnbondLockIn.Release(12L + delegatee.UnbondingPeriod);
            Assert.Empty(delegation.UnbondLockIn.Entries);
        }

        [Fact]
        public void Redelegate()
        {
            var delegator = _fixture.TestDelegator1;
            var delegatee1 = _fixture.TestDelegatee1;
            var delegatee2 = _fixture.TestDelegatee2;
            var delegation1 = _fixture.Delegation1To1;
            var delegation2 = _fixture.Delegation1To2;
            delegator.Delegate(delegatee1, delegatee1.Currency * 10, delegation1);
            Assert.Equal(delegatee1.Address, Assert.Single(delegator.Delegatees));

            var redelegatingShare = delegation1.Bond.Share / 2;
            var redelegatingFAV = delegatee1.FAVToUnbond(redelegatingShare);
            delegator.Redelegate(
                delegatee1, delegatee2, redelegatingShare, 10L, delegation1, delegation2);
            Assert.Equal(redelegatingFAV, delegation2.IncompleteBond);
            Assert.Equal(2, delegator.Delegatees.Count);
            var entriesByExpireHeight = Assert.Single(delegation1.RebondGrace.Entries);
            Assert.Equal(10L + delegatee1.UnbondingPeriod, entriesByExpireHeight.Key);
            var entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(delegatee2.Address, entry.RebondeeAddress);
            Assert.Equal(redelegatingFAV, entry.InitialGraceFAV);
            Assert.Equal(redelegatingFAV, entry.GraceFAV);
            Assert.Equal(10L, entry.CreationHeight);
            Assert.Equal(10L + delegatee1.UnbondingPeriod, entry.ExpireHeight);
            delegation2.Complete();

            redelegatingShare = delegation1.Bond.Share;
            redelegatingFAV = delegatee1.FAVToUnbond(redelegatingShare);
            delegator.Redelegate(
                delegatee1, delegatee2, redelegatingShare, 12L, delegation1, delegation2);
            Assert.Equal(redelegatingFAV, delegation2.IncompleteBond);
            Assert.Equal(delegatee2.Address, Assert.Single(delegator.Delegatees));
            Assert.Equal(2, delegation1.RebondGrace.Entries.Count);

            delegation1.RebondGrace.Release(10L + delegatee1.UnbondingPeriod - 1);
            Assert.Equal(2, delegation1.RebondGrace.Entries.Count);

            delegation1.RebondGrace.Release(10L + delegatee1.UnbondingPeriod);
            entriesByExpireHeight = Assert.Single(delegation1.RebondGrace.Entries);
            Assert.Equal(12L + delegatee1.UnbondingPeriod, entriesByExpireHeight.Key);
            entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(delegatee2.Address, entry.RebondeeAddress);
            Assert.Equal(redelegatingFAV, entry.InitialGraceFAV);
            Assert.Equal(redelegatingFAV, entry.GraceFAV);
            Assert.Equal(12L, entry.CreationHeight);
            Assert.Equal(12L + delegatee1.UnbondingPeriod, entry.ExpireHeight);
            delegation2.Complete();

            delegation1.RebondGrace.Release(12L + delegatee1.UnbondingPeriod);
            Assert.Empty(delegation1.RebondGrace.Entries);
        }
    }
}
