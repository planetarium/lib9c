using System.Linq;
using Libplanet.Action.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    /// <summary>
    /// Counts player progress for the global counters and confirms world openings whose
    /// conditions are met.
    /// </summary>
    public static class WorldOpenCounter
    {
        /// <summary>
        /// Adds <paramref name="delta"/> to every counter of <paramref name="type"/> covering
        /// <paramref name="target"/> that a pending opening references, then confirms the
        /// openings whose conditions are now met. Writes nothing when no sheet defines such a
        /// counter, so ordinary actions cost nothing extra.
        /// </summary>
        /// <remarks>
        /// Matching counters are processed one at a time in ascending id (not CSV order), and
        /// whether an opening is still pending is re-read for each counter. So if counter A's
        /// tick confirms opening X, a later counter B that only X references is not written in
        /// the same call: X is no longer pending by then. A counter shared by several pending
        /// openings is written once per call, not once per opening.
        /// </remarks>
        /// <param name="states">The world to update.</param>
        /// <param name="type">What happened.</param>
        /// <param name="target">The stage or recipe id it happened to.</param>
        /// <param name="delta">How much to add; a non-positive delta writes nothing.</param>
        /// <param name="avatarLevel">The acting avatar's level, checked against
        /// <c>min_avatar_level</c>.</param>
        /// <param name="blockIndex">The block being evaluated; recorded as the confirmed block.
        /// </param>
        /// <returns>The updated world, or <paramref name="states"/> itself when nothing was
        /// written.</returns>
        public static IWorld Tick(
            IWorld states,
            GlobalCounterType type,
            int target,
            long delta,
            int avatarLevel,
            long blockIndex)
        {
            if (delta <= 0 || !states.TryGetPatchedSheet<GlobalCounterSheet>(out var counters))
            {
                return states;
            }

            var matching = counters.Matching(type, target)
                .Where(c => avatarLevel >= c.MinAvatarLevel)
                .ToList();
            if (matching.Count == 0
                || !states.TryGetPatchedSheet<WorldOpenSheet>(out var opens)
                || !states.TryGetPatchedSheet<WorldOpenConditionSheet>(out var conditions))
            {
                return states;
            }

            foreach (var counter in matching)
            {
                var pendingOpenIds = conditions.OpenIdsReferencing(counter.Id)
                    .Where(id => opens.TryGetValue(id, out var row) && states.IsPending(row, blockIndex))
                    .ToList();
                if (pendingOpenIds.Count == 0)
                {
                    continue;
                }

                states = states.AddCounter(counter.Id, delta);
                foreach (var openId in pendingOpenIds)
                {
                    if (IsSatisfied(states, conditions, openId))
                    {
                        states = states.ConfirmOpen(openId, blockIndex);
                    }
                }
            }

            return states;
        }

        /// <summary>
        /// Confirms every pending opening whose conditions are already met, after
        /// <paramref name="tableName"/> was patched. A table patch can lower a threshold below a
        /// counter that no longer moves often (a rarely cleared stage, a rarely crafted recipe);
        /// confirming here means such an opening does not wait for the next matching play. Only a
        /// <see cref="WorldOpenSheet"/> or <see cref="WorldOpenConditionSheet"/> patch triggers it;
        /// any other sheet, or absent world-open sheets, writes nothing.
        /// </summary>
        /// <param name="states">The world after the patched sheet was stored.</param>
        /// <param name="tableName">The patched sheet's name.</param>
        /// <param name="blockIndex">The block being evaluated; recorded as the confirmed block.
        /// </param>
        /// <returns>The updated world, or <paramref name="states"/> itself when nothing was
        /// written.</returns>
        public static IWorld ConfirmSatisfiedAfterPatch(
            IWorld states,
            string tableName,
            long blockIndex)
        {
            // Only these two decide whether an opening is satisfied and still pending; the counter
            // sheet defines what is counted, not the counts or thresholds.
            if (tableName != nameof(WorldOpenSheet)
                && tableName != nameof(WorldOpenConditionSheet))
            {
                return states;
            }

            if (!states.TryGetPatchedSheet<WorldOpenSheet>(out var opens)
                || !states.TryGetPatchedSheet<WorldOpenConditionSheet>(out var conditions)
                || opens.OrderedList is null)
            {
                return states;
            }

            foreach (var open in opens.OrderedList)
            {
                if (states.IsPending(open, blockIndex) && IsSatisfied(states, conditions, open.Id))
                {
                    states = states.ConfirmOpen(open.Id, blockIndex);
                }
            }

            return states;
        }

        /// <summary>
        /// Whether any condition group of <paramref name="openId"/> is fully met (all conditions in
        /// a group must hold; any one group suffices). An opening without condition rows is never
        /// satisfied.
        /// </summary>
        public static bool IsSatisfied(
            IWorldState state,
            WorldOpenConditionSheet conditions,
            int openId) =>
            conditions.GroupsOf(openId)
                .Any(group => group.All(c => state.GetCounter(c.CounterId) >= c.Threshold));
    }
}
