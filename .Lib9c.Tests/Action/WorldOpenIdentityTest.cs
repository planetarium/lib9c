namespace Lib9c.Tests.Action
{
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using Nekoyume.Action;
    using Nekoyume.TableData;
    using Xunit;

    public class WorldOpenIdentityTest
    {
        private const string CounterHeader = "id,type,target_begin,target_end,min_avatar_level\n";

        private const string OpenHeader =
            "id,world_id,stage_end,prerequisite_open_id,min_block_index,fallback_block_index,effective_delay_block\n";

        private const string CondHeader = "open_id,condition_group,counter_id,threshold\n";

        [Theory]
        [InlineData("1,STAGE_PLAY,10,10,0\n")] // same
        [InlineData("1,STAGE_PLAY,10,10,0\n2,RECIPE_CRAFT,5,5,0\n")] // new id added
        [InlineData("2,RECIPE_CRAFT,5,5,0\n")] // id 1 retired
        public void Counter_KeepsIdentity_Accepted(string next)
        {
            var s = Patch(NewWorld(), nameof(GlobalCounterSheet), CounterHeader + "1,STAGE_PLAY,10,10,0\n");
            Patch(s, nameof(GlobalCounterSheet), CounterHeader + next);
        }

        [Theory]
        [InlineData("1,STAGE_FIRST_CLEAR,10,10,0\n")] // type
        [InlineData("1,STAGE_PLAY,9,10,0\n")] // target_begin
        [InlineData("1,STAGE_PLAY,10,11,0\n")] // target_end
        [InlineData("1,STAGE_PLAY,10,10,50\n")] // min_avatar_level
        public void Counter_ChangedIdentity_Rejected(string next)
        {
            var s = Patch(NewWorld(), nameof(GlobalCounterSheet), CounterHeader + "1,STAGE_PLAY,10,10,0\n");
            Assert.Throws<SheetRowValidateException>(
                () => Patch(s, nameof(GlobalCounterSheet), CounterHeader + next));
        }

        [Fact]
        public void Counter_RetiredIdBackWithOtherIdentity_Rejected()
        {
            var s = Patch(NewWorld(), nameof(GlobalCounterSheet), CounterHeader + "1,STAGE_PLAY,10,10,0\n");
            s = Patch(s, nameof(GlobalCounterSheet), CounterHeader + "2,STAGE_PLAY,20,20,0\n");
            Assert.Throws<SheetRowValidateException>(
                () => Patch(s, nameof(GlobalCounterSheet), CounterHeader + "1,STAGE_PLAY,30,30,0\n"));
        }

        [Fact]
        public void Counter_RetiredIdBackWithSameIdentity_Accepted()
        {
            var s = Patch(NewWorld(), nameof(GlobalCounterSheet), CounterHeader + "1,STAGE_PLAY,10,10,0\n");
            s = Patch(s, nameof(GlobalCounterSheet), CounterHeader + "2,STAGE_PLAY,20,20,0\n");
            Patch(s, nameof(GlobalCounterSheet), CounterHeader + "1,STAGE_PLAY,10,10,0\n2,STAGE_PLAY,20,20,0\n");
        }

        [Fact]
        public void Open_WindowAndPrerequisiteAdjustable()
        {
            var s = Patch(NewWorld(), nameof(WorldOpenSheet), OpenHeader + "1,12,570,,0,1000,0\n2,12,600,1,0,2000,0\n");

            // Insert id 3 between them: id 2's prerequisite moves to 3, and every block cell moves.
            Patch(
                s,
                nameof(WorldOpenSheet),
                OpenHeader + "1,12,570,,10,3000,5\n3,12,580,1,0,1500,0\n2,12,600,3,20,4000,7\n");
        }

        [Theory]
        [InlineData("1,13,570,,0,1000,0\n")] // world_id
        [InlineData("1,12,560,,0,1000,0\n")] // stage_end
        public void Open_ChangedIdentity_Rejected(string next)
        {
            var s = Patch(NewWorld(), nameof(WorldOpenSheet), OpenHeader + "1,12,570,,0,1000,0\n");
            Assert.Throws<SheetRowValidateException>(
                () => Patch(s, nameof(WorldOpenSheet), OpenHeader + next));
        }

        [Fact]
        public void Open_ReplacedByNewIdOnSameStage_Accepted()
        {
            var s = Patch(NewWorld(), nameof(WorldOpenSheet), OpenHeader + "1,12,570,,0,1000,0\n");
            Patch(s, nameof(WorldOpenSheet), OpenHeader + "2,12,570,,0,1000,0\n");
        }

        [Theory]
        [InlineData("1,1,1,99\n1,1,2,3\n1,2,3,1\n")] // thresholds
        [InlineData("1,2,3,1\n1,7,2,5\n1,7,1,10\n")] // group numbers and row order
        [InlineData("2,1,4,1\n")] // open 1 retired, open 2 added
        public void Conditions_KeepIdentity_Accepted(string next)
        {
            var s = Patch(NewWorld(), nameof(WorldOpenConditionSheet), CondHeader + "1,1,1,10\n1,1,2,5\n1,2,3,1\n");
            Patch(s, nameof(WorldOpenConditionSheet), CondHeader + next);
        }

        [Theory]
        [InlineData("1,1,1,10\n1,1,2,5\n1,2,4,1\n")] // counter swapped
        [InlineData("1,1,1,10\n1,1,2,5\n")] // group removed
        [InlineData("1,1,1,10\n1,1,2,5\n1,1,3,1\n")] // OR turned into AND
        [InlineData("1,1,1,10\n1,1,2,5\n1,2,3,1\n1,3,4,1\n")] // group added
        public void Conditions_ChangedIdentity_Rejected(string next)
        {
            var s = Patch(NewWorld(), nameof(WorldOpenConditionSheet), CondHeader + "1,1,1,10\n1,1,2,5\n1,2,3,1\n");
            Assert.Throws<SheetRowValidateException>(
                () => Patch(s, nameof(WorldOpenConditionSheet), CondHeader + next));
        }

        [Fact]
        public void OtherSheet_WritesNothing()
        {
            var s = NewWorld();
            Assert.Same(s, WorldOpenIdentity.CheckAndRecord(s, "StageSheet", "id,cost\n1,5\n"));
        }

        [Fact]
        public void NothingNew_ReturnsSameWorld()
        {
            var s = Patch(NewWorld(), nameof(WorldOpenSheet), OpenHeader + "1,12,570,,0,1000,0\n");
            Assert.Same(s, Patch(s, nameof(WorldOpenSheet), OpenHeader + "1,12,570,,0,9000,0\n"));
        }

        private static IWorld Patch(IWorld states, string tableName, string csv)
        {
            TableSheetPatchValidation.Validate(tableName, csv);
            return WorldOpenIdentity.CheckAndRecord(states, tableName, csv);
        }

        private static IWorld NewWorld() => new World(MockUtil.MockModernWorldState);
    }
}
