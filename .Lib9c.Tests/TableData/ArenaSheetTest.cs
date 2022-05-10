namespace Lib9c.Tests.TableData
{
    using System.Linq;
    using Nekoyume.TableData;
    using Xunit;

    public class ArenaSheetTest
    {
        [Fact]
        public void SetToSheet()
        {
            const string content = @"id,round,arena_type,start_index,end_index,required_wins,entrance_fee,ticket_price,additional_ticket_price
1,1,0,0,10,0,5,5,2
1,2,0,11,20,0,6,5,2
1,3,1,21,40,5,7,10,15
2,1,0,100,110,0,8,5,2
2,2,0,111,120,0,9,5,2
2,3,1,21,140,5,10,10,15";

            var sheet = new ArenaSheet();
            sheet.Set(content);

            Assert.Equal(2, sheet.Count);
            Assert.Equal(3, sheet.First.Round.Count);
            Assert.Equal(1, sheet.First.Round.First().Id);
            Assert.Equal(1, sheet.First.Round.First().Round);
            Assert.Equal(0, sheet.First.Round.First().ArenaType);
            Assert.Equal(0, sheet.First.Round.First().StartIndex);
            Assert.Equal(10, sheet.First.Round.First().EndIndex);
            Assert.Equal(0, sheet.First.Round.First().RequiredWins);
            Assert.Equal(5, sheet.First.Round.First().EntranceFee);
            Assert.Equal(5, sheet.First.Round.First().TicketPrice);
            Assert.Equal(2, sheet.First.Round.First().AdditionalTicketPrice);
        }
    }
}
