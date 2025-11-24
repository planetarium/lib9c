namespace Lib9c.Tests.Model.State
{
    using System;
    using System.Linq;
    using Libplanet.Crypto;
    using Nekoyume.Model.InfiniteTower;
    using Nekoyume.TableData;
    using Xunit;

    public class InfiniteTowerBoardStateTest
    {
        private readonly TableSheets _tableSheets;

        public InfiniteTowerBoardStateTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void SeasonStart_Grants_One_Ticket()
        {
            var avatarAddress = new PrivateKey().Address;
            var itInfo = CreateInfiniteTowerInfo(avatarAddress, 1);

            // simulate season start via PerformSeasonReset
            itInfo.PerformSeasonReset(currentBlockIndex: 100, dailyFreeTickets: 5, maxTickets: 10);

            Assert.Equal(1, itInfo.RemainingTickets);
            Assert.Equal(100, itInfo.LastResetBlockIndex);
            Assert.Equal(100, itInfo.LastTicketRefillBlockIndex);
        }

        [Fact]
        public void Refill_Periodic_With_Cap_10()
        {
            var avatarAddress = new PrivateKey().Address;
            var itInfo = CreateInfiniteTowerInfo(avatarAddress, 1);

            // season start: 1 ticket
            itInfo.PerformSeasonReset(currentBlockIndex: 0, dailyFreeTickets: 5, maxTickets: 10);
            Assert.Equal(1, itInfo.RemainingTickets);

            // first TryRefillDailyTickets initializes reference when called first time after reset
            var didRefill = itInfo.TryRefillDailyTickets(dailyFreeTickets: 5, maxTickets: 10, currentBlockIndex: 1, blocksPerDay: 10);
            Assert.False(didRefill);

            // advance one period (10 blocks) -> +5 tickets, total = 6
            didRefill = itInfo.TryRefillDailyTickets(dailyFreeTickets: 5, maxTickets: 10, currentBlockIndex: 11, blocksPerDay: 10);
            Assert.True(didRefill);
            Assert.Equal(6, itInfo.RemainingTickets);

            // advance two periods (20 blocks), expect +10 but capped to 10 total
            didRefill = itInfo.TryRefillDailyTickets(dailyFreeTickets: 5, maxTickets: 10, currentBlockIndex: 31, blocksPerDay: 10);
            Assert.True(didRefill);
            Assert.Equal(10, itInfo.RemainingTickets);

            // further periods shouldn't increase beyond 10
            didRefill = itInfo.TryRefillDailyTickets(dailyFreeTickets: 5, maxTickets: 10, currentBlockIndex: 41, blocksPerDay: 10);
            Assert.False(didRefill);
            Assert.Equal(10, itInfo.RemainingTickets);
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
