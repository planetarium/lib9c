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
        /// <paramref name="blockIndex"/>. A missing sheet leaves gated stages locked.
        /// </summary>
        public static bool IsStageOpen(
            IWorldState state,
            WorldSheet.Row worldRow,
            int stageId,
            long blockIndex)
        {
            if (stageId < GameConfig.ProgressGatedStageBegin)
            {
                return true;
            }

            return AppliedStageEnd(state, worldRow, blockIndex) >= stageId;
        }

        /// <summary>
        /// The last enterable stage of <paramref name="worldRow"/>;
        /// <c>StageBegin - 1</c> when all of it is locked.
        /// </summary>
        public static int GetOpenedStageEnd(
            IWorldState state,
            WorldSheet.Row worldRow,
            long blockIndex)
        {
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
