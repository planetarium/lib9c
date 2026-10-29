using System;
using System.Collections.Generic;
using System.Globalization;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.TableData;

namespace Nekoyume.Module
{
    /// <summary>
    /// Records when hard world openings were confirmed and decides whether they are in effect.
    /// </summary>
    public static class WorldOpenModule
    {
        /// <summary>
        /// The state key of opening <paramref name="openId"/>.
        /// </summary>
        public static Address OpenKey(int openId) =>
            Addresses.WorldOpen.Derive($"open_{openId.ToString(CultureInfo.InvariantCulture)}");

        /// <summary>
        /// Gets the block at which opening <paramref name="openId"/> was confirmed.
        /// </summary>
        public static bool TryGetConfirmedBlock(this IWorldState state, int openId, out long block)
        {
            block = 0;
            if (state.GetAccountState(Addresses.WorldOpen).GetState(OpenKey(openId)) is not Integer i)
            {
                return false;
            }

            block = i;
            return true;
        }

        /// <summary>
        /// Confirms the opening at <paramref name="blockIndex"/>. Never reverts: an already
        /// confirmed opening is left as it is.
        /// </summary>
        public static IWorld ConfirmOpen(this IWorld world, int openId, long blockIndex)
        {
            if (world.TryGetConfirmedBlock(openId, out _))
            {
                return world;
            }

            var account = world.GetAccount(Addresses.WorldOpen);
            return world.SetAccount(
                Addresses.WorldOpen,
                account.SetState(OpenKey(openId), (Integer)blockIndex));
        }

        /// <summary>
        /// Whether <paramref name="open"/> and all its prerequisites are in effect at
        /// <paramref name="block"/>. Iterative with a visited set (a cycle must not overflow the
        /// stack); a cycle, an unknown prerequisite or an invalid row (see
        /// <see cref="WorldOpenSheet.Row.IsValid"/>) anywhere in the chain means not applied.
        /// </summary>
        public static bool IsApplied(
            this IWorldState state,
            WorldOpenSheet sheet,
            WorldOpenSheet.Row open,
            long block)
        {
            var visited = new HashSet<int>();
            for (var row = open; row is not null;)
            {
                if (!visited.Add(row.Id))
                {
                    return false;
                }

                if (!row.IsValid || !IsSelfApplied(state, row, block))
                {
                    return false;
                }

                if (row.PrerequisiteOpenId is not int pre)
                {
                    return true;
                }

                if (!sheet.TryGetValue(pre, out row))
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// The first block at which <see cref="IsApplied"/> becomes true for
        /// <paramref name="open"/>, given the current state: the latest over the prerequisite
        /// chain of each row's own effective block (its fallback when unconfirmed, otherwise
        /// <c>min(fallback, max(confirmed + delay, min))</c>). Confirming an opening later can
        /// only bring this block earlier, never later.
        /// </summary>
        /// <returns>
        /// <see langword="false"/> when the chain has a cycle, an unknown prerequisite or an
        /// invalid row, i.e. when <see cref="IsApplied"/> is false at every block.
        /// </returns>
        public static bool TryGetAppliedBlock(
            this IWorldState state,
            WorldOpenSheet sheet,
            WorldOpenSheet.Row open,
            out long appliedBlock)
        {
            appliedBlock = 0;
            var latest = 0L;
            var visited = new HashSet<int>();
            for (var row = open; row is not null;)
            {
                if (!visited.Add(row.Id) || !row.IsValid)
                {
                    return false;
                }

                latest = Math.Max(latest, SelfAppliedBlock(state, row));
                if (row.PrerequisiteOpenId is not int pre)
                {
                    appliedBlock = latest;
                    return true;
                }

                if (!sheet.TryGetValue(pre, out row))
                {
                    return false;
                }
            }

            return false;
        }

        // Written without confirmed + delay so a huge delay cannot overflow into an early open.
        // block and confirmed are both non-negative, so block - confirmed cannot overflow.
        private static bool IsSelfApplied(IWorldState state, WorldOpenSheet.Row r, long block) =>
            block >= r.FallbackBlockIndex
            || (state.TryGetConfirmedBlock(r.Id, out var confirmed)
                && block - confirmed >= r.EffectiveDelayBlock
                && block >= r.MinBlockIndex);

        // The smallest block at which IsSelfApplied holds; mirrors it term by term.
        private static long SelfAppliedBlock(IWorldState state, WorldOpenSheet.Row r)
        {
            if (!state.TryGetConfirmedBlock(r.Id, out var confirmed))
            {
                return r.FallbackBlockIndex;
            }

            var delayed = r.EffectiveDelayBlock > long.MaxValue - confirmed
                ? long.MaxValue
                : confirmed + r.EffectiveDelayBlock;
            return Math.Min(r.FallbackBlockIndex, Math.Max(delayed, r.MinBlockIndex));
        }

        /// <summary>
        /// Whether the opening is valid, unconfirmed and before its fallback, i.e. counters for
        /// it may still be written.
        /// </summary>
        public static bool IsPending(this IWorldState state, WorldOpenSheet.Row r, long block) =>
            r.IsValid && !state.TryGetConfirmedBlock(r.Id, out _) && block < r.FallbackBlockIndex;
    }
}
