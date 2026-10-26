using System;
using Libplanet.Action.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    /// <summary>
    /// Decides whether a stage may be entered. Stages from
    /// <see cref="GameConfig.ProgressGatedStageBegin"/> are locked until an applied
    /// <see cref="WorldOpenSheet"/> row covers them.
    /// </summary>
    public static class WorldOpenGate
    {
        /// <summary>
        /// Whether <paramref name="stageId"/> of <paramref name="worldRow"/> is open at
        /// <paramref name="blockIndex"/>. A missing sheet leaves gated stages locked. Mimisbrunnr
        /// is never gated. Precondition: the caller has already validated that
        /// <paramref name="stageId"/> lies within <c>StageBegin..StageEnd</c> of
        /// <paramref name="worldRow"/>.
        /// </summary>
        /// <param name="state">The world to read the sheet and confirmed openings from.</param>
        /// <param name="worldRow">The world the stage belongs to.</param>
        /// <param name="stageId">The stage to check.</param>
        /// <param name="blockIndex">The block the entry is evaluated at. Inside an action this is
        /// <c>context.BlockIndex</c>; a caller outside the chain predicting whether its next
        /// action will pass must pass <c>tip + 1</c>, the block that action lands in at the
        /// earliest, not the tip itself.</param>
        public static bool IsStageOpen(
            IWorldState state,
            WorldSheet.Row worldRow,
            int stageId,
            long blockIndex)
        {
            if (worldRow.Id == GameConfig.MimisbrunnrWorldId
                || stageId < GameConfig.ProgressGatedStageBegin)
            {
                return true;
            }

            return AppliedStageEnd(state, worldRow, blockIndex) >= stageId;
        }

        /// <summary>
        /// The last enterable stage of <paramref name="worldRow"/>;
        /// <c>StageBegin - 1</c> when all of it is locked.
        /// </summary>
        /// <remarks>
        /// Global only: this ignores whether the avatar has unlocked the world or cleared the
        /// previous stage. Combine it with the avatar's own progress to know what that avatar
        /// can enter.
        /// </remarks>
        /// <param name="state">The world to read the sheet and confirmed openings from.</param>
        /// <param name="worldRow">The world to check.</param>
        /// <param name="blockIndex">The block to evaluate at. A caller predicting the next
        /// action must pass <c>tip + 1</c>, not the tip (see <see cref="IsStageOpen"/>).</param>
        public static int GetOpenedStageEnd(
            IWorldState state,
            WorldSheet.Row worldRow,
            long blockIndex)
        {
            if (worldRow.Id == GameConfig.MimisbrunnrWorldId)
            {
                return worldRow.StageEnd;
            }

            var ungated = Math.Max(
                worldRow.StageBegin - 1,
                Math.Min(worldRow.StageEnd, GameConfig.ProgressGatedStageBegin - 1));
            return Math.Min(
                worldRow.StageEnd,
                Math.Max(ungated, AppliedStageEnd(state, worldRow, blockIndex)));
        }

        private static int AppliedStageEnd(IWorldState state, WorldSheet.Row worldRow, long block)
        {
            if (!state.TryGetPatchedSheet<WorldOpenSheet>(out var sheet))
            {
                return int.MinValue;
            }

            var end = int.MinValue;
            foreach (var row in sheet.ByWorld(worldRow.Id))
            {
                if (row.IsValid && row.StageEnd > end && state.IsApplied(sheet, row, block))
                {
                    end = row.StageEnd;
                }
            }

            return end;
        }
    }
}
