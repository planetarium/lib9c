namespace Lib9c.Tests.Action
{
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class WorldOpenGateTest
    {
        private const string Header =
            "id,world_id,stage_end,prerequisite_open_id,min_block_index,fallback_block_index,effective_delay_block\n";

        [Fact]
        public void NoSheet_UngatedStage_Open_GatedStage_Closed()
        {
            var state = NewWorld();
            Assert.True(WorldOpenGate.IsStageOpen(state, World(11, 501, 520), 510, 0));
            Assert.False(WorldOpenGate.IsStageOpen(state, World(11, 501, 550), 530, 0));
            Assert.True(WorldOpenGate.IsStageOpen(state, World(1, 1, 50), 50, 0));
        }

        [Fact]
        public void Gate_OnlyOpenSheet_NoConditions_OpensAtFallback()
        {
            var state = WithSheet(NewWorld(), "1,11,550,,0,1000,0\n");
            var world = World(11, 501, 550);
            Assert.False(WorldOpenGate.IsStageOpen(state, world, 530, 999));
            Assert.True(WorldOpenGate.IsStageOpen(state, world, 530, 1000));
            Assert.True(WorldOpenGate.IsStageOpen(state, world, 550, 1000));
            Assert.False(WorldOpenGate.IsStageOpen(state, World(12, 551, 600), 551, 1000));
        }

        [Fact]
        public void SecondHalfNeedsFirst()
        {
            var state = WithSheet(NewWorld(), "1,12,570,,0,100000,0\n2,12,600,1,0,100000,0\n").ConfirmOpen(1, 5);
            var world = World(12, 551, 600);
            Assert.True(WorldOpenGate.IsStageOpen(state, world, 570, 50));
            Assert.False(WorldOpenGate.IsStageOpen(state, world, 571, 50));
        }

        [Fact]
        public void GetOpenedStageEnd()
        {
            var state = NewWorld();
            Assert.Equal(520, WorldOpenGate.GetOpenedStageEnd(state, World(11, 501, 550), 0));
            Assert.Equal(550, WorldOpenGate.GetOpenedStageEnd(state, World(12, 551, 600), 0));
            var opened = WithSheet(state, "1,12,570,,0,10,0\n");
            Assert.Equal(570, WorldOpenGate.GetOpenedStageEnd(opened, World(12, 551, 600), 10));
            Assert.Equal(550, WorldOpenGate.GetOpenedStageEnd(opened, World(12, 551, 600), 9));
        }

        [Fact]
        public void Mimisbrunnr_IsNeverGated()
        {
            var state = NewWorld();
            var row = World(GameConfig.MimisbrunnrWorldId, 10000001, 10000020);
            Assert.True(WorldOpenGate.IsStageOpen(state, row, GameConfig.MimisbrunnrStartStageId, 0));
            Assert.Equal(10000020, WorldOpenGate.GetOpenedStageEnd(state, row, 0));
        }

        [Fact]
        public void MaxStageEndOfAppliedRowsWins()
        {
            var state = WithSheet(NewWorld(), "1,12,570,,0,10,0\n2,12,600,,0,10,0\n3,12,580,,0,10,0\n");
            var world = World(12, 551, 600);
            Assert.Equal(600, WorldOpenGate.GetOpenedStageEnd(state, world, 10));
            Assert.True(WorldOpenGate.IsStageOpen(state, world, 600, 10));
        }

        [Fact]
        public void StageNotOpenedException_IsInvalidStage()
        {
            var e = new StageNotOpenedException(530, 11);
            Assert.IsAssignableFrom<InvalidStageException>(e);
            Assert.Equal("stage 530 of world 11 is not opened yet", e.Message);
        }

        private static IWorld NewWorld() => new World(MockUtil.MockModernWorldState);

        private static IWorld WithSheet(IWorld world, string rows) =>
            world.SetLegacyState(Addresses.GetSheetAddress<WorldOpenSheet>(), (Text)(Header + rows));

        private static WorldSheet.Row World(int id, int begin, int end)
        {
            var sheet = new WorldSheet();
            sheet.Set($"id,name,stage_begin,stage_end\n{id},w,{begin},{end}\n");
            return sheet[id];
        }
    }
}
