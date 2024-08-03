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
            var delegatee = new TestDelegatee(address);
            Assert.Equal(address, delegatee.Address);
            Assert.Equal(DelegationFixture.TestCurrency, delegatee.Currency);
            Assert.Equal(3, delegatee.UnbondingPeriod);
            Assert.Equal(new byte[] { 0x01 }, delegatee.DelegateeId);
        }

        [Fact]
        public void CtorWithBencoded()
        {
            var address = new Address("0xe8327129891e1A0B2E3F0bfa295777912295942a");
            var delegatee = _fixture.TestDelegatee1;
            var delegator = _fixture.TestDelegator1;
            var bond = _fixture.Bond1To1;
            delegatee.Bond(delegator, delegatee.Currency * 10, bond);

            var delegateeRecon = new TestDelegatee(address, delegatee.Bencoded);
            Assert.Equal(address, delegateeRecon.Address);
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
            var bond1To1 = _fixture.Bond1To1;
            var bond2To1 = _fixture.Bond2To1;

            var share1 = BigInteger.Zero;
            var share2 = BigInteger.Zero;
            var totalShare = BigInteger.Zero;
            var totalBonding = testDelegatee.Currency * 0;

            var bonding = testDelegatee.Currency * 10;
            var share = testDelegatee.ShareToBond(bonding);
            share1 += share;
            totalShare += share;
            totalBonding += bonding;

            var bondResult = testDelegatee.Bond(testDelegator1, bonding, bond1To1);
            bond1To1 = bondResult.Bond;
            Assert.Equal(testDelegator1.Address, Assert.Single(testDelegatee.Delegators));
            Assert.Equal(share, bondResult.BondedShare);
            Assert.Equal(share1, bondResult.Bond.Share);
            Assert.Equal(totalShare, testDelegatee.TotalShares);
            Assert.Equal(totalBonding, testDelegatee.TotalDelegated);

            bonding = testDelegatee.Currency * 20;
            share = testDelegatee.ShareToBond(bonding);
            share1 += share;
            totalShare += share;
            totalBonding += bonding;
            bondResult = testDelegatee.Bond(testDelegator1, bonding, bond1To1);
            Assert.Equal(testDelegator1.Address, Assert.Single(testDelegatee.Delegators));
            Assert.Equal(share, bondResult.BondedShare);
            Assert.Equal(share1, bondResult.Bond.Share);
            Assert.Equal(totalShare, testDelegatee.TotalShares);
            Assert.Equal(totalBonding, testDelegatee.TotalDelegated);

            bonding = testDelegatee.Currency * 30;
            share = testDelegatee.ShareToBond(bonding);
            share2 += share;
            totalShare += share;
            totalBonding += bonding;
            bondResult = testDelegatee.Bond(testDelegator2, bonding, bond2To1);
            Assert.Equal(2, testDelegatee.Delegators.Count);
            Assert.Contains(testDelegator1.Address, testDelegatee.Delegators);
            Assert.Contains(testDelegator2.Address, testDelegatee.Delegators);
            Assert.Equal(share, bondResult.BondedShare);
            Assert.Equal(share2, bondResult.Bond.Share);
            Assert.Equal(totalShare, testDelegatee.TotalShares);
            Assert.Equal(totalBonding, testDelegatee.TotalDelegated);
        }

        [Fact]
        public void CannotBondInvalidDelegator()
        {
            IDelegatee testDelegatee = _fixture.TestDelegatee1;
            var testDelegator = _fixture.TestDelegator1;
            var dummyDelegator = _fixture.DummyDelegator1;
            var bond = _fixture.Bond1To1;

            Assert.Throws<InvalidCastException>(
                () => testDelegatee.Bond(
                    dummyDelegator, testDelegatee.Currency * 10, bond));
        }

        [Fact]
        public void CannotBondInvalidCurrency()
        {
            var testDelegatee = _fixture.TestDelegatee1;
            var testDelegator = _fixture.TestDelegator1;
            var dummyDelegator = _fixture.DummyDelegator1;
            var bond = _fixture.Bond1To1;
            var invalidCurrency = Currency.Uncapped("invalid", 3, null);

            Assert.Throws<InvalidOperationException>(
                () => testDelegatee.Bond(
                    testDelegator, invalidCurrency * 10, bond));
        }

        [Fact]
        public void Unbond()
        {
            var testDelegatee = _fixture.TestDelegatee1;
            var testDelegator1 = _fixture.TestDelegator1;
            var testDelegator2 = _fixture.TestDelegator2;
            var bond1To1 = _fixture.Bond1To1;
            var bond2To1 = _fixture.Bond2To1;

            var share1 = BigInteger.Zero;
            var share2 = BigInteger.Zero;
            var totalShares = BigInteger.Zero;
            var totalDelegated = testDelegatee.Currency * 0;

            var bonding = testDelegatee.Currency * 100;
            var share = testDelegatee.ShareToBond(bonding);
            share1 += share;
            totalShares += share;
            totalDelegated += bonding;
            bond1To1 = testDelegatee.Bond(testDelegator1, bonding, bond1To1).Bond;

            bonding = testDelegatee.Currency * 50;
            share = testDelegatee.ShareToBond(bonding);
            share2 += share;
            totalShares += share;
            totalDelegated += bonding;
            bond2To1 = testDelegatee.Bond(testDelegator2, bonding, bond2To1).Bond;

            var unbonding = share1 / 2;
            share1 -= unbonding;
            totalShares -= unbonding;
            var unbondingFAV = testDelegatee.FAVToUnbond(unbonding);
            totalDelegated -= unbondingFAV;
            var unbondResult = testDelegatee.Unbond(testDelegator1, unbonding, bond1To1);
            bond1To1 = unbondResult.Bond;
            Assert.Equal(2, testDelegatee.Delegators.Count);
            Assert.Contains(testDelegator1.Address, testDelegatee.Delegators);
            Assert.Contains(testDelegator2.Address, testDelegatee.Delegators);
            Assert.Equal(unbondingFAV, unbondResult.UnbondedFAV);
            Assert.Equal(share1, unbondResult.Bond.Share);
            Assert.Equal(totalShares, testDelegatee.TotalShares);
            Assert.Equal(totalDelegated, testDelegatee.TotalDelegated);

            unbonding = share2 / 2;
            share2 -= unbonding;
            totalShares -= unbonding;
            unbondingFAV = testDelegatee.FAVToUnbond(unbonding);
            totalDelegated -= unbondingFAV;
            unbondResult = testDelegatee.Unbond(testDelegator2, unbonding, bond2To1);
            Assert.Equal(2, testDelegatee.Delegators.Count);
            Assert.Contains(testDelegator1.Address, testDelegatee.Delegators);
            Assert.Contains(testDelegator2.Address, testDelegatee.Delegators);
            Assert.Equal(unbondingFAV, unbondResult.UnbondedFAV);
            Assert.Equal(share2, unbondResult.Bond.Share);
            Assert.Equal(totalShares, testDelegatee.TotalShares);
            Assert.Equal(totalDelegated, testDelegatee.TotalDelegated);

            totalShares -= share1;
            unbondingFAV = testDelegatee.FAVToUnbond(share1);
            totalDelegated -= unbondingFAV;
            unbondResult = testDelegatee.Unbond(testDelegator1, share1, bond1To1);
            Assert.Equal(testDelegator2.Address, Assert.Single(testDelegatee.Delegators));
            Assert.Equal(unbondingFAV, unbondResult.UnbondedFAV);
            Assert.Equal(BigInteger.Zero, unbondResult.Bond.Share);
            Assert.Equal(totalShares, testDelegatee.TotalShares);
            Assert.Equal(totalDelegated, testDelegatee.TotalDelegated);
        }

        [Fact]
        public void CannotUnbondInvalidDelegator()
        {
            IDelegatee delegatee = _fixture.TestDelegatee1;
            Assert.Throws<InvalidCastException>(
                () => delegatee.Unbond(
                    _fixture.DummyDelegator1, BigInteger.One, _fixture.Bond1To1));
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
                testDelegatee1.RewardPoolAddress,
                dummyDelegatee1.RewardPoolAddress);
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
