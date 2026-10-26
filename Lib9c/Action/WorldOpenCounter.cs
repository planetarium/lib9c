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
        /// Whether any condition group of <paramref name="openId"/> is fully met (conditions of
        /// a group are ANDed, groups are ORed). An opening without condition rows is never
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
