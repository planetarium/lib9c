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

        private static IWorld NewWorld() => new World(MockUtil.MockModernWorldState);

        private static WorldOpenSheet Sheet(string rows)
        {
            var sheet = new WorldOpenSheet();
            sheet.Set(Header + rows);
            return sheet;
        }
    }
}
