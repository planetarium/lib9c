namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Action.Exceptions;
    using Nekoyume.Model.InfiniteTower;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class InfiniteTowerScheduleTest
    {
        private readonly TableSheets _tableSheets;

        public InfiniteTowerScheduleTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void TicketRow_IsActive_BeforeStart_ShouldReturnFalse()
        {
            // Arrange
            var scheduleRow = CreateTestScheduleRow(1000, 2000);
            var currentBlockIndex = 500L;

            // Act
            var result = scheduleRow.IsActive(currentBlockIndex);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TicketRow_IsActive_DuringSchedule_ShouldReturnTrue()
        {
            // Arrange
            var scheduleRow = CreateTestScheduleRow(1000, 2000);
            var currentBlockIndex = 1500L;

            // Act
            var result = scheduleRow.IsActive(currentBlockIndex);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TicketRow_IsActive_AfterEnd_ShouldReturnFalse()
        {
            // Arrange
            var scheduleRow = CreateTestScheduleRow(1000, 2000);
            var currentBlockIndex = 2500L;

            // Act
            var result = scheduleRow.IsActive(currentBlockIndex);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TicketRow_HasStarted_BeforeStart_ShouldReturnFalse()
        {
            // Arrange
            var scheduleRow = CreateTestScheduleRow(1000, 2000);
            var currentBlockIndex = 500L;

            // Act
            var result = scheduleRow.HasStarted(currentBlockIndex);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TicketRow_HasStarted_AtStart_ShouldReturnTrue()
        {
            // Arrange
            var scheduleRow = CreateTestScheduleRow(1000, 2000);
            var currentBlockIndex = 1000L;

            // Act
            var result = scheduleRow.HasStarted(currentBlockIndex);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TicketRow_HasStarted_AfterStart_ShouldReturnTrue()
        {
            // Arrange
            var scheduleRow = CreateTestScheduleRow(1000, 2000);
            var currentBlockIndex = 1500L;

            // Act
            var result = scheduleRow.HasStarted(currentBlockIndex);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TicketRow_HasEnded_BeforeEnd_ShouldReturnFalse()
        {
            // Arrange
            var scheduleRow = CreateTestScheduleRow(1000, 2000);
            var currentBlockIndex = 1500L;

            // Act
            var result = scheduleRow.HasEnded(currentBlockIndex);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TicketRow_HasEnded_AtEnd_ShouldReturnFalse()
        {
            // Arrange
            var scheduleRow = CreateTestScheduleRow(1000, 2000);
            var currentBlockIndex = 2000L;

            // Act
            var result = scheduleRow.HasEnded(currentBlockIndex);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TicketRow_HasEnded_AfterEnd_ShouldReturnTrue()
        {
            // Arrange
            var scheduleRow = CreateTestScheduleRow(1000, 2000);
            var currentBlockIndex = 2500L;

            // Act
            var result = scheduleRow.HasEnded(currentBlockIndex);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TicketRow_GetRemainingBlocks_DuringSchedule_ShouldReturnCorrectValue()
        {
            // Arrange
            var scheduleRow = CreateTestScheduleRow(1000, 2000);
            var currentBlockIndex = 1500L;

            // Act
            var result = scheduleRow.GetRemainingBlocks(currentBlockIndex);

            // Assert
            Assert.Equal(500L, result);
        }

        [Fact]
        public void TicketRow_GetRemainingBlocks_AfterEnd_ShouldReturnZero()
        {
            // Arrange
            var scheduleRow = CreateTestScheduleRow(1000, 2000);
            var currentBlockIndex = 2500L;

            // Act
            var result = scheduleRow.GetRemainingBlocks(currentBlockIndex);

            // Assert
            Assert.Equal(0L, result);
        }

        [Fact]
        public void InfiniteTowerInfo_IsAccessible_DuringSchedule_ShouldReturnTrue()
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
        public void InfiniteTowerInfo_IsAccessible_BeforeSchedule_ShouldReturnFalse()
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
        public void InfiniteTowerInfo_IsAccessible_AfterSchedule_ShouldReturnFalse()
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
        public void InfiniteTowerInfo_GetScheduleStatus_BeforeStart_ShouldReturnNotStarted()
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
        public void InfiniteTowerInfo_GetScheduleStatus_DuringSchedule_ShouldReturnActive()
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
        public void InfiniteTowerInfo_GetScheduleStatus_AfterEnd_ShouldReturnEnded()
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

        private InfiniteTowerScheduleSheet.Row CreateTestScheduleRow(long startBlockIndex, long endBlockIndex)
        {
            var fields = new List<string>
            {
                "1", // Id
                "1", // InfiniteTowerId
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
