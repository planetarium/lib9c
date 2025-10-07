namespace Lib9c.Tests.Model.Stake
{
    using System;
    using Lib9c.Model.Stake;
    using Lib9c.Model.State;
    using Libplanet.Crypto;
    using Xunit;

    public class StakeStateTest
    {
        [Fact]
        public void DeriveAddress()
        {
            var agentAddr = new PrivateKey().Address;
            var expectedStakeStateAddr = LegacyStakeState.DeriveAddress(agentAddr);
            Assert.Equal(expectedStakeStateAddr, StakeState.DeriveAddress(agentAddr));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(long.MaxValue, long.MaxValue)]
        public void Constructor(long startedBlockIndex, long receivedBlockIndex)
        {
            var contract = new Contract(
                Contract.StakeRegularFixedRewardSheetPrefix,
                Contract.StakeRegularRewardSheetPrefix,
                1,
                1);
            var state = new StakeState(contract, startedBlockIndex, receivedBlockIndex);
            Assert.Equal(contract, state.Contract);
            Assert.Equal(startedBlockIndex, state.StartedBlockIndex);
            Assert.Equal(receivedBlockIndex, state.ReceivedBlockIndex);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(long.MaxValue, long.MaxValue)]
        public void Constructor_Throw_ArgumentNullException(
            long startedBlockIndex,
            long receivedBlockIndex)
        {
            Assert.Throws<ArgumentNullException>(() =>
                new StakeState(null, startedBlockIndex, receivedBlockIndex));
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(-1, -1)]
        public void Constructor_Throw_ArgumentOutOfRangeException(
            long startedBlockIndex,
            long receivedBlockIndex)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StakeState(null, startedBlockIndex, receivedBlockIndex));
        }

        [Theory]
        [InlineData(0, null)]
        [InlineData(0, 0)]
        [InlineData(long.MaxValue, null)]
        [InlineData(long.MaxValue, long.MaxValue)]
        public void Constructor_With_StakeState(long startedBlockIndex, long? receivedBlockIndex)
        {
            var stakeState = new LegacyStakeState(
                new PrivateKey().Address,
                startedBlockIndex);
            if (receivedBlockIndex.HasValue)
            {
                stakeState.Claim(receivedBlockIndex.Value);
            }

            var contract = new Contract(
                Contract.StakeRegularFixedRewardSheetPrefix,
                Contract.StakeRegularRewardSheetPrefix,
                1,
                1);
            var stakeStateV2 = new StakeState(stakeState, contract);
            Assert.Equal(contract, stakeStateV2.Contract);
            Assert.Equal(stakeState.StartedBlockIndex, stakeStateV2.StartedBlockIndex);
            Assert.Equal(stakeState.ReceivedBlockIndex, stakeStateV2.ReceivedBlockIndex);
        }

        [Fact]
        public void Constructor_With_StakeState_Throw_ArgumentNullException()
        {
            var stakeState = new LegacyStakeState(new PrivateKey().Address, 0);
            var contract = new Contract(
                Contract.StakeRegularFixedRewardSheetPrefix,
                Contract.StakeRegularRewardSheetPrefix,
                1,
                1);
            Assert.Throws<ArgumentNullException>(() => new StakeState(null, contract));
            Assert.Throws<ArgumentNullException>(() => new StakeState(stakeState, null));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(long.MaxValue, long.MaxValue)]
        public void Serde(long startedBlockIndex, long receivedBlockIndex)
        {
            var contract = new Contract(
                Contract.StakeRegularFixedRewardSheetPrefix,
                Contract.StakeRegularRewardSheetPrefix,
                1,
                1);
            var state = new StakeState(contract, startedBlockIndex, receivedBlockIndex);
            var ser = state.Serialize();
            var des = new StakeState(ser);
            Assert.Equal(state.Contract, des.Contract);
            Assert.Equal(state.StartedBlockIndex, des.StartedBlockIndex);
            Assert.Equal(state.ReceivedBlockIndex, des.ReceivedBlockIndex);
            var ser2 = des.Serialize();
            Assert.Equal(ser, ser2);
        }

        [Fact]
        public void Compare()
        {
            var contract = new Contract(
                Contract.StakeRegularFixedRewardSheetPrefix,
                Contract.StakeRegularRewardSheetPrefix,
                1,
                1);
            var stateL = new StakeState(contract, 0);
            var stateR = new StakeState(contract, 0);
            Assert.Equal(stateL, stateR);
            Assert.True(stateL == stateR);
            stateR = new StakeState(contract, 1);
            Assert.NotEqual(stateL, stateR);
            Assert.True(stateL != stateR);
        }
    }
}
