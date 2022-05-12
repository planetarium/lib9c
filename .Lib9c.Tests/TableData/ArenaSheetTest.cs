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
            const string content = @"id,round,arena_type,start_block_index,end_block_index,required_wins,entrance_fee,ticket_price,additional_ticket_price
1,1,0,0,5,0,0,5,2
1,2,0,6,10,0,0,5,2
1,3,1,11,20000,1,0,5,2";

            var sheet = new ArenaSheet();
            sheet.Set(content);

            Assert.Single(sheet);
            Assert.Equal(3, sheet.First.Round.Count);
            Assert.Equal(1, sheet.First.Round.First().Id);
            Assert.Equal(1, sheet.First.Round.First().Round);
            Assert.Equal(0, sheet.First.Round.First().ArenaType);
            Assert.Equal(0, sheet.First.Round.First().StartBlockIndex);
            Assert.Equal(5, sheet.First.Round.First().EndBlockIndex);
            Assert.Equal(0, sheet.First.Round.First().RequiredWins);
            Assert.Equal(0, sheet.First.Round.First().EntranceFee);
            Assert.Equal(5, sheet.First.Round.First().TicketPrice);
            Assert.Equal(2, sheet.First.Round.First().AdditionalTicketPrice);
        }
    }
}
