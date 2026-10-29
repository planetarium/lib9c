namespace Lib9c.Tests.Module
{
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class WorldOpenModuleTest
    {
        private const string Header =
            "id,world_id,stage_end,prerequisite_open_id,min_block_index,fallback_block_index,effective_delay_block\n";

        [Fact]
        public void Counter_Add_And_Get()
        {
            var world = NewWorld();
            Assert.Equal(0L, world.GetCounter(5));
            world = world.AddCounter(5, 3).AddCounter(5, 4);
            Assert.Equal(7L, world.GetCounter(5));
            Assert.Equal(0L, world.GetCounter(6));
        }

        [Fact]
        public void ConfirmOpen_Keeps_First()
        {
            var world = NewWorld();
            Assert.False(world.TryGetConfirmedBlock(1, out _));
            world = world.ConfirmOpen(1, 100).ConfirmOpen(1, 200);
            Assert.True(world.TryGetConfirmedBlock(1, out var block));
            Assert.Equal(100L, block);
        }

        [Fact]
        public void IsApplied_Fallback()
        {
            var world = NewWorld();
            var sheet = Sheet("1,1,10,,0,1000,10\n");
            Assert.False(world.IsApplied(sheet, sheet[1], 999));
            Assert.True(world.IsApplied(sheet, sheet[1], 1000));
        }

        [Fact]
        public void IsApplied_Delay_And_Min()
        {
            var world = NewWorld().ConfirmOpen(1, 100);
            var sheet = Sheet("1,1,10,,0,100000,10\n");
            Assert.False(world.IsApplied(sheet, sheet[1], 109));
            Assert.True(world.IsApplied(sheet, sheet[1], 110));

            var sheet2 = Sheet("1,1,10,,150,100000,10\n");
            Assert.False(world.IsApplied(sheet2, sheet2[1], 149));
            Assert.True(world.IsApplied(sheet2, sheet2[1], 150));
        }

        [Fact]
        public void IsApplied_Prerequisite()
        {
            var sheet = Sheet("1,1,10,,0,100000,0\n2,1,20,1,0,100000,0\n");
            var world = NewWorld().ConfirmOpen(2, 5);
            Assert.False(world.IsApplied(sheet, sheet[2], 50));
            world = world.ConfirmOpen(1, 5);
            Assert.True(world.IsApplied(sheet, sheet[2], 50));
        }

        [Fact]
        public void IsApplied_Cycle_And_Missing_Are_False()
        {
            var sheet = Sheet("1,1,10,2,0,100000,0\n2,1,20,1,0,100000,0\n3,1,30,99,0,100000,0\n");
            var world = NewWorld().ConfirmOpen(1, 1).ConfirmOpen(2, 1).ConfirmOpen(3, 1);
            Assert.False(world.IsApplied(sheet, sheet[1], 50));
            Assert.False(world.IsApplied(sheet, sheet[3], 50));
        }

        [Fact]
        public void IsPending()
        {
            var sheet = Sheet("1,1,10,,0,1000,0\n");
            var world = NewWorld();
            Assert.True(world.IsPending(sheet[1], 10));
            Assert.False(world.IsPending(sheet[1], 1000));
            Assert.False(world.ConfirmOpen(1, 10).IsPending(sheet[1], 11));
        }

        [Fact]
        public void IsApplied_HugeDelay_StaysClosed_Until_Fallback()
        {
            // confirmed + delay would overflow to a negative block and open immediately.
            var sheet = Sheet($"1,1,10,,0,1000,{long.MaxValue}\n");
            var world = NewWorld().ConfirmOpen(1, 100);
            Assert.False(world.IsApplied(sheet, sheet[1], 101));
            Assert.False(world.IsApplied(sheet, sheet[1], 999));
            Assert.True(world.IsApplied(sheet, sheet[1], 1000));
        }

        [Fact]
        public void IsApplied_InvalidRow_Is_Never_Applied()
        {
            // fallback does not parse, so it reads as 0 and would count as applied from block 0.
            var sheet = Sheet("1,1,10,,0,x,0\n2,1,20,1,0,1000,0\n");
            Assert.False(sheet[1].IsValid);
            var world = NewWorld().ConfirmOpen(1, 1).ConfirmOpen(2, 1);
            Assert.False(world.IsApplied(sheet, sheet[1], 0));
            Assert.False(world.IsApplied(sheet, sheet[1], long.MaxValue));
            Assert.False(world.IsApplied(sheet, sheet[2], 2000));
        }

        [Fact]
        public void IsPending_InvalidRow_Is_False()
        {
            var sheet = Sheet("1,1,10,,0,1000,x\n");
            Assert.False(sheet[1].IsValid);
            Assert.False(NewWorld().IsPending(sheet[1], 10));
        }

        [Theory]
        [InlineData(false, 0L, 0L, 1000L, 10L, 1000L)] // unconfirmed: fallback
        [InlineData(true, 100L, 0L, 1000L, 10L, 110L)] // confirmed + delay
        [InlineData(true, 100L, 150L, 1000L, 10L, 150L)] // min later than confirmed + delay
        [InlineData(true, 995L, 0L, 1000L, 10L, 1000L)] // fallback earlier than confirmed + delay
        [InlineData(true, 100L, 0L, 1000L, long.MaxValue, 1000L)] // overflow saturates at fallback
        [InlineData(true, 0L, 0L, 1000L, 0L, 0L)] // confirmed at genesis, no delay
        public void TryGetAppliedBlock_Single(
            bool confirmed,
            long confirmedBlock,
            long min,
            long fallback,
            long delay,
            long expected)
        {
            var sheet = Sheet($"1,1,10,,{min},{fallback},{delay}\n");
            var world = NewWorld();
            if (confirmed)
            {
                world = world.ConfirmOpen(1, confirmedBlock);
            }

            Assert.True(world.TryGetAppliedBlock(sheet, sheet[1], out var applied));
            Assert.Equal(expected, applied);
            AssertConsistent(world, sheet, sheet[1], applied);
        }

        [Fact]
        public void TryGetAppliedBlock_Prerequisite_Takes_The_Latest()
        {
            var sheet = Sheet("1,1,10,,0,5000,0\n2,1,20,1,0,100000,10\n3,1,30,2,0,100000,0\n");

            // 2 confirmed early, 1 unconfirmed: 2 waits for 1's fallback.
            var world = NewWorld().ConfirmOpen(2, 100);
            Assert.True(world.TryGetAppliedBlock(sheet, sheet[2], out var applied));
            Assert.Equal(5000L, applied);
            AssertConsistent(world, sheet, sheet[2], applied);

            // 1 confirmed: 2 opens at its own confirmed + delay.
            world = world.ConfirmOpen(1, 50);
            Assert.True(world.TryGetAppliedBlock(sheet, sheet[2], out applied));
            Assert.Equal(110L, applied);
            AssertConsistent(world, sheet, sheet[2], applied);

            // 3 confirmed before 2 took effect: 3 waits for 2.
            world = world.ConfirmOpen(3, 105);
            Assert.True(world.TryGetAppliedBlock(sheet, sheet[3], out applied));
            Assert.Equal(110L, applied);
            AssertConsistent(world, sheet, sheet[3], applied);
        }

        [Fact]
        public void TryGetAppliedBlock_False_On_Cycle_Missing_Invalid()
        {
            var sheet = Sheet(
                "1,1,10,2,0,1000,0\n2,1,20,1,0,1000,0\n3,1,30,99,0,1000,0\n" +
                "4,1,40,,0,x,0\n5,1,50,4,0,1000,0\n");
            var world = NewWorld();
            foreach (var id in new[] { 1, 3, 4, 5 })
            {
                Assert.False(world.TryGetAppliedBlock(sheet, sheet[id], out _));
                Assert.False(world.IsApplied(sheet, sheet[id], long.MaxValue));
            }
        }

        private static void AssertConsistent(
            IWorld world,
            WorldOpenSheet sheet,
            WorldOpenSheet.Row open,
            long applied)
        {
            Assert.True(world.IsApplied(sheet, open, applied));
            if (applied > 0)
            {
                Assert.False(world.IsApplied(sheet, open, applied - 1));
            }
        }

        private static IWorld NewWorld() => new World(MockUtil.MockModernWorldState);

        private static WorldOpenSheet Sheet(string rows)
        {
            var sheet = new WorldOpenSheet();
            sheet.Set(Header + rows);
            return sheet;
        }
    }
}
