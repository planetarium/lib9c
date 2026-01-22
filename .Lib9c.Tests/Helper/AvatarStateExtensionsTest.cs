namespace Lib9c.Tests.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Crypto;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Event;
    using Xunit;

    /// <summary>
    /// Unit tests for AvatarStateExtensions helper methods.
    /// </summary>
    public class AvatarStateExtensionsTest
    {
        private readonly TableSheets _tableSheets;
        private readonly AvatarState _avatarState;

        public AvatarStateExtensionsTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            _avatarState = AvatarState.Create(
                new PrivateKey().Address,
                new PrivateKey().Address,
                0,
                _tableSheets.GetAvatarSheets(),
                new PrivateKey().Address
            );
        }

        [Theory]
        [InlineData(1, 0, 1, 1, 0)] // Level 1, 0 exp, 1 play -> no level up
        [InlineData(1, 5, 1, 1, 5)] // Level 1, 5 exp, 1 play -> no level up
        [InlineData(2, 0, 1, 2, 0)] // Level 2, 0 exp, 1 play -> no level up
        public void GetLevelAndExpForEventDungeon_NoLevelUp(
            int initialLevel, int initialExp, int playCount, int expectedLevel, int expectedExp)
        {
            // Arrange
            _avatarState.level = initialLevel;
            _avatarState.exp = initialExp;
            var scheduleRow = _tableSheets.EventScheduleSheet[1001];
            var stageNumber = 1;
            var stageExp = scheduleRow.GetStageExp(stageNumber, 1);

            // Act
            var (level, exp) = _avatarState.GetLevelAndExpForEventDungeon(
                _tableSheets.CharacterLevelSheet,
                scheduleRow,
                stageNumber,
                playCount);

            // Assert
            Assert.Equal(expectedLevel, level);
            Assert.Equal(expectedExp + stageExp * playCount, exp);
        }

        [Theory]
        [InlineData(1, 0, 8, 1001, 11, 2)] // Level 1, 0 exp, 8 plays, stage 11 -> level up to 2
        [InlineData(1, 5, 10, 1001, 11, 2)] // Level 1, 5 exp, 10 plays, stage 11 -> level up to 2
        [InlineData(1, 0, 30, 1001, 11, 3)] // Level 1, 0 exp, 30 plays, stage 11 -> level up to 3
        public void GetLevelAndExpForEventDungeon_WithLevelUp(
            int initialLevel, int initialExp, int playCount, int scheduleId, int stageNumber, int expectedMinLevel)
        {
            // Arrange
            _avatarState.level = initialLevel;
            _avatarState.exp = initialExp;
            var scheduleRow = _tableSheets.EventScheduleSheet[scheduleId];
            var levelSheet = _tableSheets.CharacterLevelSheet;

            // Act
            var (level, exp) = _avatarState.GetLevelAndExpForEventDungeon(
                levelSheet,
                scheduleRow,
                stageNumber,
                playCount);

            // Assert
            Assert.True(level >= expectedMinLevel, $"Expected level >= {expectedMinLevel}, but got {level}");
            Assert.True(exp >= initialExp, $"Final experience ({exp}) should be >= initial experience ({initialExp})");

            // Verify experience is valid for the final level
            if (levelSheet.TryGetValue(level, out var levelRow, true))
            {
                var maxExp = levelRow.Exp + levelRow.ExpNeed;
                Assert.True(exp < maxExp, $"Final experience ({exp}) should be < max exp for level {level} ({maxExp})");
                Assert.True(exp >= levelRow.Exp, $"Final experience ({exp}) should be >= starting exp for level {level} ({levelRow.Exp})");
            }
        }

        /// <summary>
        /// Tests that GetLevelAndExpForEventDungeon correctly limits experience at maximum level.
        /// </summary>
        [Fact]
        public void GetLevelAndExpForEventDungeon_MaxLevelLimit()
        {
            // Arrange
            var maxLevel = _tableSheets.CharacterLevelSheet.OrderedList.Last().Level;
            _avatarState.level = maxLevel;
            _avatarState.exp = 0;
            var scheduleRow = _tableSheets.EventScheduleSheet[1001];
            var stageNumber = 1;
            var levelSheet = _tableSheets.CharacterLevelSheet;
            var maxLevelRow = levelSheet.OrderedList.LastOrDefault();
            Assert.NotNull(maxLevelRow);
            var maxExp = maxLevelRow.Exp + maxLevelRow.ExpNeed - 1;

            // Act
            var (level, exp) = _avatarState.GetLevelAndExpForEventDungeon(
                levelSheet,
                scheduleRow,
                stageNumber,
                100); // Large play count

            // Assert
            Assert.Equal(maxLevel, level);
            Assert.True(exp <= maxExp, $"Experience at max level should be <= {maxExp}, but got {exp}");
        }

        [Theory]
        [InlineData(1, 0, 50, 1001, 11)] // Level 1, 0 exp, 50 plays, stage 11 -> multiple level ups
        [InlineData(1, 0, 100, 1001, 11)] // Level 1, 0 exp, 100 plays, stage 11 -> many level ups
        public void GetLevelAndExpForEventDungeon_MultipleLevelUps(
            int initialLevel, int initialExp, int playCount, int scheduleId, int stageNumber)
        {
            // Arrange
            _avatarState.level = initialLevel;
            _avatarState.exp = initialExp;
            var scheduleRow = _tableSheets.EventScheduleSheet[scheduleId];
            var levelSheet = _tableSheets.CharacterLevelSheet;

            // Act
            var (level, exp) = _avatarState.GetLevelAndExpForEventDungeon(
                levelSheet,
                scheduleRow,
                stageNumber,
                playCount);

            // Assert
            Assert.True(level > initialLevel, $"Level should have increased from {initialLevel} to {level}");

            // Verify experience is valid for the final level
            if (levelSheet.TryGetValue(level, out var levelRow, true))
            {
                var maxExp = levelRow.Exp + levelRow.ExpNeed;
                Assert.True(exp < maxExp, $"Final experience ({exp}) should be < max exp for level {level} ({maxExp})");
                Assert.True(exp >= levelRow.Exp, $"Final experience ({exp}) should be >= starting exp for level {level} ({levelRow.Exp})");
            }
        }

        /// <summary>
        /// Tests that GetLevelAndExpForEventDungeon correctly handles zero play count.
        /// </summary>
        [Fact]
        public void GetLevelAndExpForEventDungeon_ZeroPlayCount()
        {
            // Arrange
            _avatarState.level = 1;
            _avatarState.exp = 5;
            var scheduleRow = _tableSheets.EventScheduleSheet[1001];
            var stageNumber = 1;
            var levelSheet = _tableSheets.CharacterLevelSheet;

            // Act
            var (level, exp) = _avatarState.GetLevelAndExpForEventDungeon(
                levelSheet,
                scheduleRow,
                stageNumber,
                0); // Zero play count

            // Assert
            Assert.Equal(1, level);
            Assert.Equal(5, exp); // Experience should remain unchanged
        }

        [Theory]
        [InlineData(1, 1, 5)] // Stage 1
        [InlineData(11, 1, 5)] // Stage 11
        [InlineData(20, 1, 5)] // Stage 20
        public void GetLevelAndExpForEventDungeon_DifferentStages(
            int stageNumber, int initialLevel, int playCount)
        {
            // Arrange
            _avatarState.level = initialLevel;
            _avatarState.exp = 0;
            var scheduleRow = _tableSheets.EventScheduleSheet[1001];
            var levelSheet = _tableSheets.CharacterLevelSheet;
            var stageExp = scheduleRow.GetStageExp(stageNumber, 1);

            // Act
            var (level, exp) = _avatarState.GetLevelAndExpForEventDungeon(
                levelSheet,
                scheduleRow,
                stageNumber,
                playCount);

            // Assert
            if (stageExp > 0)
            {
                Assert.True(
                    exp >= stageExp * playCount || level > initialLevel,
                    $"Experience should be gained or level should increase");
            }
        }
    }
}
