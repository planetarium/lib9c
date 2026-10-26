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

    public class WorldOpenCounterTest
    {
        private const string CounterHeader = "id,type,target_begin,target_end,min_avatar_level\n";

        private const string OpenHeader =
            "id,world_id,stage_end,prerequisite_open_id,min_block_index,fallback_block_index,effective_delay_block\n";

        private const string CondHeader = "open_id,condition_group,counter_id,threshold\n";

        private const GlobalCounterType Clear = GlobalCounterType.STAGE_FIRST_CLEAR;

        [Fact]
        public void SingleCondition_ConfirmsAtThreshold()
        {
            var s = Build("1,STAGE_FIRST_CLEAR,10,10,0\n", "1,12,570,,0,1000,0\n", "1,1,1,2\n");
            s = WorldOpenCounter.Tick(s, Clear, 10, 1, 300, 5);
            Assert.Equal(1, s.GetCounter(1));
            Assert.False(s.TryGetConfirmedBlock(1, out _));
            s = WorldOpenCounter.Tick(s, Clear, 10, 1, 300, 6);
            Assert.Equal(2, s.GetCounter(1));
            Assert.True(s.TryGetConfirmedBlock(1, out var b));
            Assert.Equal(6, b);
        }

        [Fact]
        public void AndGroup_NeedsAll()
        {
            var s = Build(
                "1,STAGE_FIRST_CLEAR,10,10,0\n2,STAGE_PLAY,10,10,0\n",
                "1,12,570,,0,1000,0\n",
                "1,1,1,1\n1,1,2,5\n");
            s = WorldOpenCounter.Tick(s, Clear, 10, 1, 300, 5);
            Assert.False(s.TryGetConfirmedBlock(1, out _));
            s = WorldOpenCounter.Tick(s, GlobalCounterType.STAGE_PLAY, 10, 5, 300, 7);
            Assert.True(s.TryGetConfirmedBlock(1, out var b));
            Assert.Equal(7, b);
        }

        [Fact]
        public void OrGroups_AnyGroupConfirms()
        {
            var s = Build(
                "1,STAGE_FIRST_CLEAR,10,10,0\n2,STAGE_PLAY,10,10,0\n",
                "1,12,570,,0,1000,0\n",
                "1,1,1,10\n1,2,2,1\n");
            s = WorldOpenCounter.Tick(s, GlobalCounterType.STAGE_PLAY, 10, 1, 300, 9);
            Assert.True(s.TryGetConfirmedBlock(1, out var b));
            Assert.Equal(9, b);
        }

        [Fact]
        public void BelowMinLevel_NoWrite()
        {
            var s = Build("1,STAGE_FIRST_CLEAR,10,10,200\n", "1,12,570,,0,1000,0\n", "1,1,1,1\n");
            s = WorldOpenCounter.Tick(s, Clear, 10, 1, 199, 5);
            Assert.Equal(0, s.GetCounter(1));
            Assert.False(s.TryGetConfirmedBlock(1, out _));
        }

        [Fact]
        public void Tick_AfterFallback_NoWrite()
        {
            var s = Build("1,STAGE_FIRST_CLEAR,10,10,0\n", "1,12,570,,0,100,0\n", "1,1,1,1\n");
            s = WorldOpenCounter.Tick(s, Clear, 10, 1, 300, 100);
            Assert.Equal(0, s.GetCounter(1));
            Assert.False(s.TryGetConfirmedBlock(1, out _));
        }

        [Fact]
        public void Tick_MissingConditionSheet_NoWrite()
        {
            var s = NewWorld()
                .SetLegacyState(Addresses.GetSheetAddress<GlobalCounterSheet>(), (Text)(CounterHeader + "1,STAGE_FIRST_CLEAR,10,10,0\n"))
                .SetLegacyState(Addresses.GetSheetAddress<WorldOpenSheet>(), (Text)(OpenHeader + "1,12,570,,0,1000,0\n"));
            s = WorldOpenCounter.Tick(s, Clear, 10, 1, 300, 5);
            Assert.Equal(0, s.GetCounter(1));
        }

        [Fact]
        public void Tick_MissingCounterSheet_NoWrite()
        {
            var s = NewWorld();
            Assert.Same(s, WorldOpenCounter.Tick(s, Clear, 10, 1, 300, 5));
        }

        [Fact]
        public void OrphanConditionRow_NoWrite()
        {
            var s = Build("1,STAGE_FIRST_CLEAR,10,10,0\n", "2,12,570,,0,1000,0\n", "1,1,1,1\n");
            s = WorldOpenCounter.Tick(s, Clear, 10, 1, 300, 5);
            Assert.Equal(0, s.GetCounter(1));
        }

        [Fact]
        public void AlreadyConfirmed_NoWrite()
        {
            var s = Build("1,STAGE_FIRST_CLEAR,10,10,0\n", "1,12,570,,0,1000,0\n", "1,1,1,5\n")
                .ConfirmOpen(1, 2);
            s = WorldOpenCounter.Tick(s, Clear, 10, 1, 300, 5);
            Assert.Equal(0, s.GetCounter(1));
        }

        [Fact]
        public void TargetOutOfRange_NoWrite()
        {
            var s = Build("1,STAGE_FIRST_CLEAR,10,10,0\n", "1,12,570,,0,1000,0\n", "1,1,1,1\n");
            s = WorldOpenCounter.Tick(s, Clear, 11, 1, 300, 5);
            Assert.Equal(0, s.GetCounter(1));
        }

        [Fact]
        public void NonPositiveDelta_NoWrite()
        {
            var s = Build("1,STAGE_FIRST_CLEAR,10,10,0\n", "1,12,570,,0,1000,0\n", "1,1,1,1\n");
            Assert.Same(s, WorldOpenCounter.Tick(s, Clear, 10, 0, 300, 5));
        }

        [Fact]
        public void IsSatisfied_NoConditionRows_False()
        {
            var s = Build("1,STAGE_FIRST_CLEAR,10,10,0\n", "1,12,570,,0,1000,0\n", "2,1,1,1\n");
            s.TryGetPatchedSheet<WorldOpenConditionSheet>(out var cond);
            Assert.False(WorldOpenCounter.IsSatisfied(s, cond, 1));
        }

        [Fact]
        public void Tick_ProcessesCountersInAscendingId_SkipsCounterOfJustConfirmedOpen()
        {
            // Counter 2 is listed first in the CSV but counter 1 is processed first. Its tick
            // confirms open 1, and counter 2 is referenced only by open 1, so open 1 is no longer
            // pending when counter 2's turn comes and counter 2 is not written in this call.
            var s = Build(
                "2,STAGE_PLAY,10,10,0\n1,STAGE_PLAY,10,10,0\n",
                "1,12,570,,0,1000,0\n",
                "1,1,1,1\n1,2,2,100\n");
            s = WorldOpenCounter.Tick(s, GlobalCounterType.STAGE_PLAY, 10, 1, 300, 5);
            Assert.True(s.TryGetConfirmedBlock(1, out var b));
            Assert.Equal(5, b);
            Assert.Equal(1, s.GetCounter(1));
            Assert.Equal(0, s.GetCounter(2));
        }

        [Fact]
        public void Tick_CounterSharedByTwoOpens()
        {
            var s = Build(
                "1,STAGE_FIRST_CLEAR,10,10,0\n",
                "1,12,570,,0,1000,0\n2,13,620,,0,1000,0\n",
                "1,1,1,1\n2,1,1,2\n");
            s = WorldOpenCounter.Tick(s, Clear, 10, 1, 300, 5);
            Assert.Equal(1, s.GetCounter(1));
            Assert.True(s.TryGetConfirmedBlock(1, out var b1));
            Assert.Equal(5, b1);
            Assert.False(s.TryGetConfirmedBlock(2, out _));

            // Still pending for open 2, so it keeps counting, once per tick.
            s = WorldOpenCounter.Tick(s, Clear, 10, 1, 300, 8);
            Assert.Equal(2, s.GetCounter(1));
            Assert.True(s.TryGetConfirmedBlock(1, out b1));
            Assert.Equal(5, b1);
            Assert.True(s.TryGetConfirmedBlock(2, out var b2));
            Assert.Equal(8, b2);

            // Both confirmed: no longer written.
            s = WorldOpenCounter.Tick(s, Clear, 10, 1, 300, 9);
            Assert.Equal(2, s.GetCounter(1));
        }

        [Fact]
        public void Tick_MissingWorldOpenSheet_NoWrite()
        {
            var s = NewWorld()
                .SetLegacyState(Addresses.GetSheetAddress<GlobalCounterSheet>(), (Text)(CounterHeader + "1,STAGE_FIRST_CLEAR,10,10,0\n"))
                .SetLegacyState(Addresses.GetSheetAddress<WorldOpenConditionSheet>(), (Text)(CondHeader + "1,1,1,1\n"));
            Assert.Same(s, WorldOpenCounter.Tick(s, Clear, 10, 1, 300, 5));
            Assert.Equal(0, s.GetCounter(1));
        }

        private static IWorld NewWorld() => new World(MockUtil.MockModernWorldState);

        private static IWorld Build(string counters, string opens, string conds) =>
            NewWorld()
                .SetLegacyState(Addresses.GetSheetAddress<GlobalCounterSheet>(), (Text)(CounterHeader + counters))
                .SetLegacyState(Addresses.GetSheetAddress<WorldOpenSheet>(), (Text)(OpenHeader + opens))
                .SetLegacyState(Addresses.GetSheetAddress<WorldOpenConditionSheet>(), (Text)(CondHeader + conds));
    }
}
