namespace Lib9c.Tests.Delegation
{
    using System;
    using System.Numerics;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Delegation;
    using Xunit;

    public class DelegateeTest
    {
        private readonly DelegationFixture _fixture;

        public DelegateeTest()
        {
            _fixture = new DelegationFixture();
        }

        [Fact]
        public void Ctor()
        {
            var address = new Address("0xe8327129891e1A0B2E3F0bfa295777912295942a");
            var delegatee = new TestDelegatee(address, _fixture.TestRepository.DelegateeAccountAddress, _fixture.TestRepository);
            Assert.Equal(address, delegatee.Address);
            Assert.Equal(DelegationFixture.TestCurrency, delegatee.DelegationCurrency);
            Assert.Equal(3, delegatee.UnbondingPeriod);
        }

        [Fact]
        public void GetSet()
        {
            var repo = _fixture.TestRepository;
            var delegatee = _fixture.TestDelegatee1;
            var delegator = _fixture.TestDelegator1;
            delegatee.Bond(delegator, delegatee.DelegationCurrency * 10, 10L);
            var delegateeRecon = repo.GetDelegatee(delegatee.Address);
            Assert.Equal(delegatee.Address, delegateeRecon.Address);
            Assert.Equal(delegator.Address, Assert.Single(delegateeRecon.Delegators));
            Assert.Equal(delegatee.TotalDelegated, delegateeRecon.TotalDelegated);
            Assert.Equal(delegatee.TotalShares, delegateeRecon.TotalShares);
        }

        [Fact]
        public void Exchange()
        {
            // TODO: Test exchange after slashing is implemented.
            // (Delegatee.ShareToBond & Delegatee.BondToShare)
        }

        [Fact]
        public void Bond()
        {
            var testDelegatee = _fixture.TestDelegatee1;
            var testDelegator1 = _fixture.TestDelegator1;
            var testDelegator2 = _fixture.TestDelegator2;

            var share1 = BigInteger.Zero;
            var share2 = BigInteger.Zero;
            var totalShare = BigInteger.Zero;
            var totalBonding = testDelegatee.DelegationCurrency * 0;

            var bonding = testDelegatee.DelegationCurrency * 10;
            var share = testDelegatee.ShareFromFAV(bonding);
            share1 += share;
            totalShare += share;
            totalBonding += bonding;

            var bondedShare = testDelegatee.Bond(testDelegator1, bonding, 10L);
            var bondedShare1 = _fixture.TestRepository.GetBond(testDelegatee, testDelegator1.Address).Share;
            Assert.Equal(testDelegator1.Address, Assert.Single(testDelegatee.Delegators));
            Assert.Equal(share, bondedShare);
            Assert.Equal(share1, bondedShare1);
            Assert.Equal(totalShare, testDelegatee.TotalShares);
            Assert.Equal(totalBonding, testDelegatee.TotalDelegated);

            bonding = testDelegatee.DelegationCurrency * 20;
            share = testDelegatee.ShareFromFAV(bonding);
            share1 += share;
            totalShare += share;
            totalBonding += bonding;
            bondedShare = testDelegatee.Bond(testDelegator1, bonding, 20L);
            bondedShare1 = _fixture.TestRepository.GetBond(testDelegatee, testDelegator1.Address).Share;
            Assert.Equal(testDelegator1.Address, Assert.Single(testDelegatee.Delegators));
            Assert.Equal(share, bondedShare);
            Assert.Equal(share1, bondedShare1);
            Assert.Equal(totalShare, testDelegatee.TotalShares);
            Assert.Equal(totalBonding, testDelegatee.TotalDelegated);

            bonding = testDelegatee.DelegationCurrency * 30;
            share = testDelegatee.ShareFromFAV(bonding);
            share2 += share;
            totalShare += share;
            totalBonding += bonding;
            bondedShare = testDelegatee.Bond(testDelegator2, bonding, 30L);
            var bondedShare2 = _fixture.TestRepository.GetBond(testDelegatee, testDelegator2.Address).Share;
            Assert.Equal(2, testDelegatee.Delegators.Count);
            Assert.Contains(testDelegator1.Address, testDelegatee.Delegators);
            Assert.Contains(testDelegator2.Address, testDelegatee.Delegators);
            Assert.Equal(share, bondedShare);
            Assert.Equal(share2, bondedShare2);
            Assert.Equal(totalShare, testDelegatee.TotalShares);
            Assert.Equal(totalBonding, testDelegatee.TotalDelegated);
        }

        [Fact]
        public void CannotBondInvalidDelegator()
        {
            var testDelegatee = _fixture.TestDelegatee1;
            var testDelegator = _fixture.TestDelegator1;
            var dummyDelegator = _fixture.DummyDelegator1;

            Assert.Throws<InvalidCastException>(
                () => testDelegatee.Bond(
                    dummyDelegator, testDelegatee.DelegationCurrency * 10, 10L));
        }

        [Fact]
        public void CannotBondInvalidCurrency()
        {
            var testDelegatee = _fixture.TestDelegatee1;
            var testDelegator = _fixture.TestDelegator1;
            var dummyDelegator = _fixture.DummyDelegator1;
            var invalidCurrency = Currency.Uncapped("invalid", 3, null);

            Assert.Throws<InvalidOperationException>(
                () => testDelegatee.Bond(
                    testDelegator, invalidCurrency * 10, 10L));
        }

        [Fact]
        public void Unbond()
        {
            var testDelegatee = _fixture.TestDelegatee1;
            var testDelegator1 = _fixture.TestDelegator1;
            var testDelegator2 = _fixture.TestDelegator2;

            var share1 = BigInteger.Zero;
            var share2 = BigInteger.Zero;
            var totalShares = BigInteger.Zero;
            var totalDelegated = testDelegatee.DelegationCurrency * 0;

            var bonding = testDelegatee.DelegationCurrency * 100;
            var share = testDelegatee.ShareFromFAV(bonding);
            share1 += share;
            totalShares += share;
            totalDelegated += bonding;
            testDelegatee.Bond(testDelegator1, bonding, 1L);

            bonding = testDelegatee.DelegationCurrency * 50;
            share = testDelegatee.ShareFromFAV(bonding);
            share2 += share;
            totalShares += share;
            totalDelegated += bonding;
            testDelegatee.Bond(testDelegator2, bonding, 2L);

            var unbonding = share1 / 2;
            share1 -= unbonding;
            totalShares -= unbonding;
            var unbondingFAV = testDelegatee.FAVFromShare(unbonding);
            totalDelegated -= unbondingFAV;
            var unbondedFAV = testDelegatee.Unbond(testDelegator1, unbonding, 3L);
            var shareAfterUnbond = _fixture.TestRepository.GetBond(testDelegatee, testDelegator1.Address).Share;
            Assert.Equal(2, testDelegatee.Delegators.Count);
            Assert.Contains(testDelegator1.Address, testDelegatee.Delegators);
            Assert.Contains(testDelegator2.Address, testDelegatee.Delegators);
            Assert.Equal(unbondingFAV, unbondedFAV);
            Assert.Equal(share1, shareAfterUnbond);
            Assert.Equal(totalShares, testDelegatee.TotalShares);
            Assert.Equal(totalDelegated, testDelegatee.TotalDelegated);

            unbonding = share2 / 2;
            share2 -= unbonding;
            totalShares -= unbonding;
            unbondingFAV = testDelegatee.FAVFromShare(unbonding);
            totalDelegated -= unbondingFAV;
            unbondedFAV = testDelegatee.Unbond(testDelegator2, unbonding, 4L);
            shareAfterUnbond = _fixture.TestRepository.GetBond(testDelegatee, testDelegator2.Address).Share;
            Assert.Equal(2, testDelegatee.Delegators.Count);
            Assert.Contains(testDelegator1.Address, testDelegatee.Delegators);
            Assert.Contains(testDelegator2.Address, testDelegatee.Delegators);
            Assert.Equal(unbondingFAV, unbondedFAV);
            Assert.Equal(share2, shareAfterUnbond);
            Assert.Equal(totalShares, testDelegatee.TotalShares);
            Assert.Equal(totalDelegated, testDelegatee.TotalDelegated);

            totalShares -= share1;
            unbondingFAV = testDelegatee.FAVFromShare(share1);
            totalDelegated -= unbondingFAV;
            unbondedFAV = testDelegatee.Unbond(testDelegator1, share1, 5L);
            shareAfterUnbond = _fixture.TestRepository.GetBond(testDelegatee, testDelegator1.Address).Share;
            Assert.Equal(testDelegator2.Address, Assert.Single(testDelegatee.Delegators));
            Assert.Equal(unbondingFAV, unbondedFAV);
            Assert.Equal(BigInteger.Zero, shareAfterUnbond);
            Assert.Equal(totalShares, testDelegatee.TotalShares);
            Assert.Equal(totalDelegated, testDelegatee.TotalDelegated);
        }

        [Fact]
        public void CannotUnbondInvalidDelegator()
        {
            var delegatee = _fixture.TestDelegatee1;
            Assert.Throws<InvalidCastException>(
                () => delegatee.Unbond(
                    _fixture.DummyDelegator1, BigInteger.One, 10L));
        }

        [Fact]
        public void ClearRemainderRewards()
        {
            var repo = _fixture.TestRepository;
            var testDelegatee = _fixture.TestDelegatee1;
            var testDelegator1 = _fixture.TestDelegator1;
            var testDelegator2 = _fixture.TestDelegator2;

            var bonding1 = testDelegatee.DelegationCurrency * 3;
            var bonding2 = testDelegatee.DelegationCurrency * 8;

            var bondedShare1 = testDelegatee.Bond(testDelegator1, bonding1, 10L);
            var bondedShare2 = testDelegatee.Bond(testDelegator2, bonding2, 10L);

            repo.MintAsset(testDelegatee.RewardPoolAddress, testDelegatee.RewardCurrency * 10);
            testDelegatee.CollectRewards(11L);

            testDelegatee.DistributeReward(testDelegator1, 11L);
            var remainder = repo.GetBalance(DelegationFixture.FixedPoolAddress, testDelegatee.RewardCurrency);
            Assert.Equal(testDelegatee.RewardCurrency * 0, remainder);

            testDelegatee.DistributeReward(testDelegator2, 11L);
            remainder = repo.GetBalance(DelegationFixture.FixedPoolAddress, testDelegatee.RewardCurrency);
            Assert.Equal(new FungibleAssetValue(testDelegatee.RewardCurrency, 0, 1), remainder);
        }

        [Fact]
        public void AddressConsistency()
        {
            var testDelegatee1 = _fixture.TestDelegatee1;
            var testDelegatee2 = _fixture.TestDelegatee2;
            var testDelegator1 = _fixture.TestDelegator1;
            var testDelegator2 = _fixture.TestDelegator2;
            var dummyDelegatee1 = _fixture.DummyDelegatee1;

            Assert.Equal(
                testDelegatee1.BondAddress(testDelegator1.Address),
                testDelegatee1.BondAddress(testDelegator1.Address));
            Assert.NotEqual(
                testDelegatee1.BondAddress(testDelegator1.Address),
                testDelegatee1.BondAddress(testDelegator2.Address));
            Assert.NotEqual(
                testDelegatee1.BondAddress(testDelegator1.Address),
                testDelegatee2.BondAddress(testDelegator1.Address));

            Assert.Equal(
                testDelegatee1.UnbondLockInAddress(testDelegator1.Address),
                testDelegatee1.UnbondLockInAddress(testDelegator1.Address));
            Assert.NotEqual(
                testDelegatee1.UnbondLockInAddress(testDelegator1.Address),
                testDelegatee1.UnbondLockInAddress(testDelegator2.Address));
            Assert.NotEqual(
                testDelegatee1.UnbondLockInAddress(testDelegator1.Address),
                testDelegatee2.UnbondLockInAddress(testDelegator1.Address));

            Assert.Equal(
                testDelegatee1.RebondGraceAddress(testDelegator1.Address),
                testDelegatee1.RebondGraceAddress(testDelegator1.Address));
            Assert.NotEqual(
                testDelegatee1.RebondGraceAddress(testDelegator1.Address),
                testDelegatee1.RebondGraceAddress(testDelegator2.Address));
            Assert.NotEqual(
                testDelegatee1.RebondGraceAddress(testDelegator1.Address),
                testDelegatee2.RebondGraceAddress(testDelegator1.Address));

            Assert.Equal(testDelegatee1.Address, dummyDelegatee1.Address);
            Assert.NotEqual(
                testDelegatee1.CurrentLumpSumRewardsRecordAddress(),
                dummyDelegatee1.CurrentLumpSumRewardsRecordAddress());
            Assert.NotEqual(
                testDelegatee1.LumpSumRewardsRecordAddress(1L),
                dummyDelegatee1.LumpSumRewardsRecordAddress(1L));
            Assert.NotEqual(
                testDelegatee1.BondAddress(testDelegator1.Address),
                dummyDelegatee1.BondAddress(testDelegator1.Address));
            Assert.NotEqual(
                testDelegatee1.UnbondLockInAddress(testDelegator1.Address),
                dummyDelegatee1.UnbondLockInAddress(testDelegator1.Address));
            Assert.NotEqual(
                testDelegatee1.RebondGraceAddress(testDelegator1.Address),
                dummyDelegatee1.RebondGraceAddress(testDelegator1.Address));
        }
    }
}
