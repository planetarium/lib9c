namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.InfiniteTower;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class InfiniteTowerIdValidationTest
    {
        private readonly TableSheets _tableSheets;

        public InfiniteTowerIdValidationTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void ValidateInfiniteTowerId_MatchingId_ShouldNotThrow()
        {
            // Arrange
            var scheduleRow = CreateTestScheduleRow(1, 1000, 2000);
            var addressesHex = "0x1234567890123456789012345678901234567890";

            // Act & Assert
            // This should not throw an exception
            ValidateInfiniteTowerIdInternal(scheduleRow, 1, addressesHex);
        }

        [Fact]
        public void ValidateInfiniteTowerId_MismatchingId_ShouldThrow()
        {
            // Arrange
            var scheduleRow = CreateTestScheduleRow(2, 1000, 2000);
            var addressesHex = "0x1234567890123456789012345678901234567890";

            // Act & Assert
            Assert.Throws<SheetRowNotFoundException>(() =>
                ValidateInfiniteTowerIdInternal(scheduleRow, 1, addressesHex));
        }

        [Fact]
        public void InfiniteTowerInfo_IsAccessible_DuringSeason_ShouldReturnTrue()
        {
            // Arrange
            var avatarAddress = new Address("0x1234567890123456789012345678901234567890");
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, 1);
            var currentBlockIndex = 1500L;
            var startBlockIndex = 1000L;
            var endBlockIndex = 2000L;

            // Act
            var result = infiniteTowerInfo.IsAccessible(currentBlockIndex, startBlockIndex, endBlockIndex);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void InfiniteTowerInfo_IsAccessible_BeforeSeason_ShouldReturnFalse()
        {
            // Arrange
            var avatarAddress = new Address("0x1234567890123456789012345678901234567890");
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, 1);
            var currentBlockIndex = 500L;
            var startBlockIndex = 1000L;
            var endBlockIndex = 2000L;

            // Act
            var result = infiniteTowerInfo.IsAccessible(currentBlockIndex, startBlockIndex, endBlockIndex);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InfiniteTowerInfo_IsAccessible_AfterSeason_ShouldReturnFalse()
        {
            // Arrange
            var avatarAddress = new Address("0x1234567890123456789012345678901234567890");
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, 1);
            var currentBlockIndex = 2500L;
            var startBlockIndex = 1000L;
            var endBlockIndex = 2000L;

            // Act
            var result = infiniteTowerInfo.IsAccessible(currentBlockIndex, startBlockIndex, endBlockIndex);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InfiniteTowerInfo_GetScheduleStatus_DuringSeason_ShouldReturnActive()
        {
            // Arrange
            var avatarAddress = new Address("0x1234567890123456789012345678901234567890");
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, 1);
            var currentBlockIndex = 1500L;
            var startBlockIndex = 1000L;
            var endBlockIndex = 2000L;

            // Act
            var result = infiniteTowerInfo.GetScheduleStatus(currentBlockIndex, startBlockIndex, endBlockIndex);

            // Assert
            Assert.Contains("Active", result);
            Assert.Contains("500", result); // 2000 - 1500 = 500 remaining
        }

        [Fact]
        public void InfiniteTowerInfo_GetScheduleStatus_BeforeSeason_ShouldReturnNotStarted()
        {
            // Arrange
            var avatarAddress = new Address("0x1234567890123456789012345678901234567890");
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, 1);
            var currentBlockIndex = 500L;
            var startBlockIndex = 1000L;
            var endBlockIndex = 2000L;

            // Act
            var result = infiniteTowerInfo.GetScheduleStatus(currentBlockIndex, startBlockIndex, endBlockIndex);

            // Assert
            Assert.Contains("Not started yet", result);
            Assert.Contains("1000", result);
        }

        [Fact]
        public void InfiniteTowerInfo_GetScheduleStatus_AfterSeason_ShouldReturnEnded()
        {
            // Arrange
            var avatarAddress = new Address("0x1234567890123456789012345678901234567890");
            var infiniteTowerInfo = CreateInfiniteTowerInfo(avatarAddress, 1);
            var currentBlockIndex = 2500L;
            var startBlockIndex = 1000L;
            var endBlockIndex = 2000L;

            // Act
            var result = infiniteTowerInfo.GetScheduleStatus(currentBlockIndex, startBlockIndex, endBlockIndex);

            // Assert
            Assert.Contains("Ended", result);
            Assert.Contains("1000", result);
            Assert.Contains("2000", result);
        }

        private InfiniteTowerScheduleSheet.Row CreateTestScheduleRow(int infiniteTowerId, long startBlockIndex, long endBlockIndex)
        {
            var fields = new List<string>
            {
                "1", // Id
                infiniteTowerId.ToString(), // InfiniteTowerId
                startBlockIndex.ToString(), // StartBlockIndex
                endBlockIndex.ToString(), // EndBlockIndex
                "5", // DailyFreeTickets
                "10", // MaxTickets
                "324000", // ResetIntervalBlocks
            };

            var row = new InfiniteTowerScheduleSheet.Row();
            row.Set(fields);
            return row;
        }

        private void ValidateInfiniteTowerIdInternal(
            InfiniteTowerScheduleSheet.Row scheduleRow,
            int expectedInfiniteTowerId,
            string addressesHex)
        {
            if (scheduleRow.InfiniteTowerId != expectedInfiniteTowerId)
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(InfiniteTowerScheduleSheet),
                    $"InfiniteTowerId mismatch. Expected: {expectedInfiniteTowerId}, Found: {scheduleRow.InfiniteTowerId}");
            }
        }

        /// <summary>
        /// Creates InfiniteTowerInfo with initial tickets from schedule sheet.
        /// </summary>
        private InfiniteTowerInfo CreateInfiniteTowerInfo(Address avatarAddress, int infiniteTowerId)
        {
            var initialTickets = 0;
            if (_tableSheets.InfiniteTowerScheduleSheet != null)
            {
                var scheduleRow = _tableSheets.InfiniteTowerScheduleSheet.Values
                    .FirstOrDefault(s => s.InfiniteTowerId == infiniteTowerId);
                if (scheduleRow != null)
                {
                    initialTickets = Math.Min(scheduleRow.DailyFreeTickets, scheduleRow.MaxTickets);
                }
            }

            return new InfiniteTowerInfo(avatarAddress, infiniteTowerId, initialTickets);
        }
    }
}
