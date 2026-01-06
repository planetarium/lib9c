namespace Lib9c.Tests.Action
{
    using System;
    using Nekoyume.Action.Exceptions;
    using Xunit;

    public class InfiniteTowerScheduleExceptionTest
    {
        [Fact]
        public void InfiniteTowerScheduleNotStartedException_ShouldContainCorrectMessage()
        {
            // Arrange
            var actionType = "InfiniteTowerBattle";
            var addressesHex = "0x1234567890123456789012345678901234567890";
            var infiniteTowerId = 1;
            var currentBlockIndex = 500L;
            var startBlockIndex = 1000L;

            // Act
            var exception = new InfiniteTowerScheduleNotStartedException(
                actionType,
                addressesHex,
                infiniteTowerId,
                currentBlockIndex,
                startBlockIndex);

            // Assert
            Assert.Contains(actionType, exception.Message);
            Assert.Contains(addressesHex, exception.Message);
            Assert.Contains(infiniteTowerId.ToString(), exception.Message);
            Assert.Contains(currentBlockIndex.ToString(), exception.Message);
            Assert.Contains(startBlockIndex.ToString(), exception.Message);
            Assert.Contains("not started yet", exception.Message);
        }

        [Fact]
        public void InfiniteTowerScheduleEndedException_ShouldContainCorrectMessage()
        {
            // Arrange
            var actionType = "InfiniteTowerBattle";
            var addressesHex = "0x1234567890123456789012345678901234567890";
            var infiniteTowerId = 1;
            var currentBlockIndex = 2500L;
            var endBlockIndex = 2000L;

            // Act
            var exception = new InfiniteTowerScheduleEndedException(
                actionType,
                addressesHex,
                infiniteTowerId,
                currentBlockIndex,
                endBlockIndex);

            // Assert
            Assert.Contains(actionType, exception.Message);
            Assert.Contains(addressesHex, exception.Message);
            Assert.Contains(infiniteTowerId.ToString(), exception.Message);
            Assert.Contains(currentBlockIndex.ToString(), exception.Message);
            Assert.Contains(endBlockIndex.ToString(), exception.Message);
            Assert.Contains("has ended", exception.Message);
        }

        [Fact]
        public void InfiniteTowerScheduleNotActiveException_ShouldContainCorrectMessage()
        {
            // Arrange
            var actionType = "InfiniteTowerBattle";
            var addressesHex = "0x1234567890123456789012345678901234567890";
            var infiniteTowerId = 1;
            var currentBlockIndex = 500L;
            var startBlockIndex = 1000L;
            var endBlockIndex = 2000L;

            // Act
            var exception = new InfiniteTowerScheduleNotActiveException(
                actionType,
                addressesHex,
                infiniteTowerId,
                currentBlockIndex,
                startBlockIndex,
                endBlockIndex);

            // Assert
            Assert.Contains(actionType, exception.Message);
            Assert.Contains(addressesHex, exception.Message);
            Assert.Contains(infiniteTowerId.ToString(), exception.Message);
            Assert.Contains(currentBlockIndex.ToString(), exception.Message);
            Assert.Contains(startBlockIndex.ToString(), exception.Message);
            Assert.Contains(endBlockIndex.ToString(), exception.Message);
            Assert.Contains("not active", exception.Message);
        }
    }
}
