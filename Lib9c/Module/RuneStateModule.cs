#nullable enable
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Module
{
    public static class RuneStateModule
    {
        public static List<RuneState> GetAllRuneState(this IWorldState worldState,
            Address avatarAddress)
        {
            var allRuneState = new List<RuneState>();
            var serialized = worldState.GetResolvedState(avatarAddress, Addresses.RuneState);
            if (serialized is null)
            {
                // Get legacy state
                var runeListSheet = worldState.GetSheet<RuneListSheet>();
                foreach (var rune in runeListSheet.Values)
                {
                    var runeAddress = RuneState.DeriveAddress(avatarAddress, rune.Id);
                    if (worldState.TryGetLegacyState(runeAddress, out List rawState))
                    {
                        allRuneState.Add(new RuneState(rawState));
                    }
                }
            }
            else
            {
                var rawStateList = (List)serialized;
                allRuneState.AddRange(rawStateList.Select(state => new RuneState((List)state)));
            }

            return allRuneState;
        }

        public static RuneState? GetRuneState(this IWorldState worldState,
            Address avatarAddress, int runeId)
        {
            var serialized = worldState.GetResolvedState(avatarAddress, Addresses.RuneState);
            if (serialized is not null)
            {
                foreach (var s in (List)serialized)
                {
                    var runeState = new RuneState((List)s);
                    if (runeState.RuneId == runeId)
                    {
                        return runeState;
                    }
                }
            }

            // Get legacy state
            return worldState.TryGetLegacyState(
                RuneState.DeriveAddress(avatarAddress, runeId),
                out List rawRuneState
            )
                ? new RuneState(rawRuneState)
                : null;
        }

        private static IValue SerializeAllRuneState(IEnumerable<RuneState> allRuneState)
        {
            return allRuneState.Aggregate(
                List.Empty,
                (current, state) => current.Add(state.Serialize())
            ).Serialize();
        }

        public static IWorld SetAllRuneState(this IWorld world,
            Address avatarAddress, IEnumerable<RuneState> allRuneState)
        {
            var account = world.GetAccount(Addresses.RuneState);
            account = account.SetState(avatarAddress, SerializeAllRuneState(allRuneState));
            return world.SetAccount(Addresses.RuneState, account);
        }

        public static IWorld SetRuneState(this IWorld world,
            Address avatarAddress, RuneState runeState)
        {
            var allRuneState = GetAllRuneState(world, avatarAddress);
            var rs = allRuneState.FirstOrDefault(s => s.RuneId == runeState.RuneId);
            if (rs is null)
            {
                rs = new RuneState(runeState.RuneId);
                allRuneState.Add(rs);
            }

            rs.LevelUp(runeState.Level - rs.Level);

            return SetAllRuneState(world, avatarAddress, allRuneState);
        }
    }
}
