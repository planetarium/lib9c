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
            var bond = _fixture.Bond1To1;
            delegator.Delegate(delegatee, delegatee.Currency * 10, bond);

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
            var bond1 = _fixture.Bond1To1;
            var bond2 = _fixture.Bond1To2;
            var delegateFAV = delegatee1.Currency * 10;
            var delegateShare = delegatee1.ShareToBond(delegateFAV);
            var result = delegator.Delegate(delegatee1, delegateFAV, bond1);
            bond1 = result.Bond;
            delegatee1 = result.Delegatee;
            Assert.Equal(delegateFAV, result.DelegatedFAV);
            Assert.Equal(delegateShare, result.Bond.Share);
            Assert.Equal(delegateFAV, delegatee1.TotalDelegated);
            Assert.Equal(delegateShare, delegatee1.TotalShares);
            Assert.Equal(delegator.Address, Assert.Single(delegatee1.Delegators));
            Assert.Equal(delegatee1.Address, Assert.Single(delegator.Delegatees));

            var delegateFAV2 = delegatee1.Currency * 20;
            var delegateShare2 = delegatee1.ShareToBond(delegateFAV2);
            result = delegator.Delegate(delegatee1, delegateFAV2, bond1);
            Assert.Equal(delegateFAV2, result.DelegatedFAV);
            Assert.Equal(delegateShare + delegateShare2, result.Bond.Share);
            Assert.Equal(delegateFAV + delegateFAV2, delegatee1.TotalDelegated);
            Assert.Equal(delegateShare + delegateShare2, delegatee1.TotalShares);
            Assert.Equal(delegator.Address, Assert.Single(delegatee1.Delegators));
            Assert.Equal(delegatee1.Address, Assert.Single(delegator.Delegatees));

            result = delegator.Delegate(delegatee2, delegateFAV, bond2);
            Assert.Equal(delegateFAV, result.DelegatedFAV);
            Assert.Equal(delegateShare, result.Bond.Share);
            Assert.Equal(delegateFAV, delegatee2.TotalDelegated);
            Assert.Equal(delegateShare, delegatee2.TotalShares);
            Assert.Equal(2, delegator.Delegatees.Count);
            Assert.Contains(delegatee1.Address, delegator.Delegatees);
            Assert.Contains(delegatee2.Address, delegator.Delegatees);
        }

        [Fact]
        public void Undelegate()
        {
            var delegator = _fixture.TestDelegator1;
            var delegatee = _fixture.TestDelegatee1;
            var bond = _fixture.Bond1To1;
            var unbondLockIn = _fixture.Unbond1To1;
            var unbondingSet = _fixture.UnbondingSet;
            var delegateResult = delegator.Delegate(
                delegatee, delegatee.Currency * 10, bond);
            delegatee = delegateResult.Delegatee;
            bond = delegateResult.Bond;

            var undelegatingShare = delegateResult.Bond.Share / 2;
            var undelegatingFAV = delegatee.FAVToUnbond(undelegatingShare);
            var undelegateResult = delegator.Undelegate(
                delegatee, undelegatingShare, 10L, bond, unbondLockIn, unbondingSet);
            delegatee = undelegateResult.Delegatee;
            bond = undelegateResult.Bond;
            unbondLockIn = undelegateResult.UnbondLockIn;
            unbondingSet = undelegateResult.UnbondingSet;
            Assert.Equal(delegatee.Address, Assert.Single(delegator.Delegatees));
            Assert.Single(unbondingSet.UnbondLockIns);
            var entriesByExpireHeight = Assert.Single(unbondLockIn.Entries);
            Assert.Equal(10L + delegatee.UnbondingPeriod, entriesByExpireHeight.Key);
            var entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(undelegatingFAV, entry.InitialLockInFAV);
            Assert.Equal(undelegatingFAV, entry.LockInFAV);
            Assert.Equal(10L, entry.CreationHeight);
            Assert.Equal(10L + delegatee.UnbondingPeriod, entry.ExpireHeight);

            undelegatingShare = bond.Share;
            undelegateResult = delegator.Undelegate(
                delegatee, undelegatingShare, 12L, bond, unbondLockIn, unbondingSet);
            delegatee = undelegateResult.Delegatee;
            bond = undelegateResult.Bond;
            unbondLockIn = undelegateResult.UnbondLockIn;
            unbondingSet = undelegateResult.UnbondingSet;
            Assert.Empty(delegator.Delegatees);
            Assert.Equal(2, unbondLockIn.Entries.Count);

            unbondLockIn = unbondLockIn.Release(10L + delegatee.UnbondingPeriod - 1);
            Assert.Equal(2, unbondLockIn.Entries.Count);

            unbondLockIn = unbondLockIn.Release(10L + delegatee.UnbondingPeriod);
            entriesByExpireHeight = Assert.Single(unbondLockIn.Entries);
            Assert.Equal(12L + delegatee.UnbondingPeriod, entriesByExpireHeight.Key);
            entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(undelegatingFAV, entry.InitialLockInFAV);
            Assert.Equal(undelegatingFAV, entry.LockInFAV);
            Assert.Equal(12L, entry.CreationHeight);
            Assert.Equal(12L + delegatee.UnbondingPeriod, entry.ExpireHeight);

            unbondLockIn = unbondLockIn.Release(12L + delegatee.UnbondingPeriod);
            Assert.Empty(unbondLockIn.Entries);
        }

        [Fact]
        public void Redelegate()
        {
            var delegator = _fixture.TestDelegator1;
            var delegatee1 = _fixture.TestDelegatee1;
            var delegatee2 = _fixture.TestDelegatee2;
            var bond1 = _fixture.Bond1To1;
            var bond2 = _fixture.Bond1To2;
            var rebondGrace = _fixture.Rebond1To1;
            var unbondingSet = _fixture.UnbondingSet;
            var delegateResult = delegator.Delegate(
                delegatee1, delegatee1.Currency * 10, bond1);
            delegatee1 = delegateResult.Delegatee;
            bond1 = delegateResult.Bond;
            Assert.Equal(delegatee1.Address, Assert.Single(delegator.Delegatees));

            var redelegatingShare = bond1.Share / 2;
            var redelegatingFAV = delegatee1.FAVToUnbond(redelegatingShare);
            var redelegateResult = delegator.Redelegate(
                delegatee1,
                delegatee2,
                redelegatingShare,
                10L,
                bond1,
                bond2,
                rebondGrace,
                unbondingSet);
            delegatee1 = redelegateResult.SrcDelegatee;
            delegatee2 = redelegateResult.DstDelegatee;
            bond1 = redelegateResult.SrcBond;
            bond2 = redelegateResult.DstBond;
            rebondGrace = redelegateResult.RebondGrace;
            unbondingSet = redelegateResult.UnbondingSet;
            Assert.Equal(2, delegator.Delegatees.Count);
            var entriesByExpireHeight = Assert.Single(rebondGrace.Entries);
            Assert.Equal(10L + delegatee1.UnbondingPeriod, entriesByExpireHeight.Key);
            var entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(delegatee2.Address, entry.RebondeeAddress);
            Assert.Equal(redelegatingFAV, entry.InitialGraceFAV);
            Assert.Equal(redelegatingFAV, entry.GraceFAV);
            Assert.Equal(10L, entry.CreationHeight);
            Assert.Equal(10L + delegatee1.UnbondingPeriod, entry.ExpireHeight);

            redelegatingShare = bond1.Share;
            redelegatingFAV = delegatee1.FAVToUnbond(redelegatingShare);
            redelegateResult = delegator.Redelegate(
                delegatee1,
                delegatee2,
                redelegatingShare,
                12L,
                bond1,
                bond2,
                rebondGrace,
                unbondingSet);
            delegatee1 = redelegateResult.SrcDelegatee;
            delegatee2 = redelegateResult.DstDelegatee;
            bond1 = redelegateResult.SrcBond;
            bond2 = redelegateResult.DstBond;
            rebondGrace = redelegateResult.RebondGrace;
            unbondingSet = redelegateResult.UnbondingSet;
            Assert.Equal(delegatee2.Address, Assert.Single(delegator.Delegatees));
            Assert.Equal(2, rebondGrace.Entries.Count);

            rebondGrace = rebondGrace.Release(10L + delegatee1.UnbondingPeriod - 1);
            Assert.Equal(2, rebondGrace.Entries.Count);

            rebondGrace = rebondGrace.Release(10L + delegatee1.UnbondingPeriod);
            entriesByExpireHeight = Assert.Single(rebondGrace.Entries);
            Assert.Equal(12L + delegatee1.UnbondingPeriod, entriesByExpireHeight.Key);
            entry = Assert.Single(entriesByExpireHeight.Value);
            Assert.Equal(delegatee2.Address, entry.RebondeeAddress);
            Assert.Equal(redelegatingFAV, entry.InitialGraceFAV);
            Assert.Equal(redelegatingFAV, entry.GraceFAV);
            Assert.Equal(12L, entry.CreationHeight);
            Assert.Equal(12L + delegatee1.UnbondingPeriod, entry.ExpireHeight);

            rebondGrace = rebondGrace.Release(12L + delegatee1.UnbondingPeriod);
            Assert.Empty(rebondGrace.Entries);
        }
    }
}
