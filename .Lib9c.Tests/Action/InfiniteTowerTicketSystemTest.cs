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

    public class InfiniteTowerTicketSystemTest
    {
        [Fact]
        public void TryRefillDailyTickets_FirstTime_ShouldNotRefill_ButInitialize()
        {
            // Arrange
            var avatarAddress = new Address("0x1234567890123456789012345678901234567890");
            var infiniteTowerInfo = new InfiniteTowerInfo(avatarAddress, 1);
            var currentBlockIndex = 1000L;
            var dailyFreeTickets = 5;
            var maxTickets = 10;

            // Act
            var result = infiniteTowerInfo.TryRefillDailyTickets(dailyFreeTickets, maxTickets, currentBlockIndex);

            // Assert
            Assert.False(result);
            Assert.Equal(0, infiniteTowerInfo.RemainingTickets);
            Assert.Equal(currentBlockIndex, infiniteTowerInfo.LastTicketRefillBlockIndex);
        }

        [Fact]
        public void TryRefillDailyTickets_NotEnoughTimePassed_ShouldNotRefill()
        {
            // Arrange
            var avatarAddress = new Address("0x1234567890123456789012345678901234567890");
            var infiniteTowerInfo = new InfiniteTowerInfo(avatarAddress, 1);
            var currentBlockIndex = 1000L;
            var dailyFreeTickets = 5;
            var maxTickets = 10;

            // First call initializes reference; no refill
            infiniteTowerInfo.TryRefillDailyTickets(dailyFreeTickets, maxTickets, currentBlockIndex);
            var initialTickets = infiniteTowerInfo.RemainingTickets; // 0

            // Act - Try to refill again before enough time has passed
            var result = infiniteTowerInfo.TryRefillDailyTickets(dailyFreeTickets, maxTickets, currentBlockIndex + 1000);

            // Assert
            Assert.False(result);
            Assert.Equal(initialTickets, infiniteTowerInfo.RemainingTickets);
        }

        [Fact]
        public void TryRefillDailyTickets_EnoughTimePassed_ShouldRefill()
        {
            // Arrange
            var avatarAddress = new Address("0x1234567890123456789012345678901234567890");
            var infiniteTowerInfo = new InfiniteTowerInfo(avatarAddress, 1);
            var currentBlockIndex = 1000L;
            var dailyFreeTickets = 5;
            var maxTickets = 10;
            var blocksPerDay = 10800;

            // First refill
            infiniteTowerInfo.TryRefillDailyTickets(dailyFreeTickets, maxTickets, currentBlockIndex);
            var initialTickets = infiniteTowerInfo.RemainingTickets;

            // Act - Try to refill after enough time has passed
            var result = infiniteTowerInfo.TryRefillDailyTickets(
                dailyFreeTickets,
                maxTickets,
                currentBlockIndex + blocksPerDay + 1,
                blocksPerDay);

            // Assert
            Assert.True(result);
            Assert.Equal(initialTickets + dailyFreeTickets, infiniteTowerInfo.RemainingTickets);
        }

        [Fact]
        public void TryRefillDailyTickets_AtMaxTickets_ShouldNotRefill()
        {
            // Arrange
            var avatarAddress = new Address("0x1234567890123456789012345678901234567890");
            var infiniteTowerInfo = new InfiniteTowerInfo(avatarAddress, 1);
            var currentBlockIndex = 1000L;
            var dailyFreeTickets = 5;
            var maxTickets = 10;

            // Set to max tickets (cap 10)
            infiniteTowerInfo.AddTickets(maxTickets);

            // Act
            var result = infiniteTowerInfo.TryRefillDailyTickets(dailyFreeTickets, maxTickets, currentBlockIndex + 20000);

            // Assert
            Assert.False(result);
            Assert.Equal(maxTickets, infiniteTowerInfo.RemainingTickets);
        }

        [Fact]
        public void PerformSeasonReset_ShouldResetAllProgress_AndGrantOneTicket()
        {
            // Arrange
            var avatarAddress = new Address("0x1234567890123456789012345678901234567890");
            var infiniteTowerInfo = new InfiniteTowerInfo(avatarAddress, 1);
            var currentBlockIndex = 1000L;
            var dailyFreeTickets = 5;
            var maxTickets = 10;

            // Set some progress
            infiniteTowerInfo.ClearFloor(5);
            infiniteTowerInfo.AddTickets(3);

            // Act
            infiniteTowerInfo.PerformSeasonReset(currentBlockIndex, dailyFreeTickets, maxTickets);

            // Assert
            Assert.Equal(0, infiniteTowerInfo.ClearedFloor);
            Assert.Equal(1, infiniteTowerInfo.RemainingTickets);
            Assert.Equal(0, infiniteTowerInfo.TotalTicketsUsed);
            Assert.Equal(0, infiniteTowerInfo.NumberOfTicketPurchases);
            Assert.Equal(currentBlockIndex, infiniteTowerInfo.LastResetBlockIndex);
            Assert.Equal(currentBlockIndex, infiniteTowerInfo.LastTicketRefillBlockIndex);
        }

        [Fact]
        public void TryUseTickets_SufficientTickets_ShouldSucceed()
        {
            // Arrange
            var avatarAddress = new Address("0x1234567890123456789012345678901234567890");
            var infiniteTowerInfo = new InfiniteTowerInfo(avatarAddress, 1);
            infiniteTowerInfo.AddTickets(5);

            // Act
            var result = infiniteTowerInfo.TryUseTickets(3);

            // Assert
            Assert.True(result);
            Assert.Equal(2, infiniteTowerInfo.RemainingTickets);
            Assert.Equal(3, infiniteTowerInfo.TotalTicketsUsed);
        }

        [Fact]
        public void TryUseTickets_InsufficientTickets_ShouldFail()
        {
            // Arrange
            var avatarAddress = new Address("0x1234567890123456789012345678901234567890");
            var infiniteTowerInfo = new InfiniteTowerInfo(avatarAddress, 1);
            infiniteTowerInfo.AddTickets(2);

            // Act
            var result = infiniteTowerInfo.TryUseTickets(5);

            // Assert
            Assert.False(result);
            Assert.Equal(2, infiniteTowerInfo.RemainingTickets);
            Assert.Equal(0, infiniteTowerInfo.TotalTicketsUsed);
        }

        [Fact]
        public void Serialize_Deserialize_ShouldPreserveState()
        {
            // Arrange
            var avatarAddress = new Address("0x1234567890123456789012345678901234567890");
            var originalInfo = new InfiniteTowerInfo(avatarAddress, 1);
            originalInfo.ClearFloor(5);
            originalInfo.AddTickets(10);

            // Act
            var serialized = originalInfo.Serialize();
            var deserialized = new InfiniteTowerInfo((Bencodex.Types.List)serialized);

            // Assert
            Assert.Equal(originalInfo.Address, deserialized.Address);
            Assert.Equal(originalInfo.InfiniteTowerId, deserialized.InfiniteTowerId);
            Assert.Equal(originalInfo.ClearedFloor, deserialized.ClearedFloor);
            Assert.Equal(originalInfo.RemainingTickets, deserialized.RemainingTickets);
            Assert.Equal(originalInfo.TotalTicketsUsed, deserialized.TotalTicketsUsed);
            Assert.Equal(originalInfo.NumberOfTicketPurchases, deserialized.NumberOfTicketPurchases);
            Assert.Equal(originalInfo.LastResetBlockIndex, deserialized.LastResetBlockIndex);
            Assert.Equal(originalInfo.LastTicketRefillBlockIndex, deserialized.LastTicketRefillBlockIndex);
        }
    }
}
