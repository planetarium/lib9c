#nullable enable
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Module
{
    /// <summary>
    /// RuneStateModule is the module to use RuneState/AllRuneState with account.
    /// </summary>
    public static class RuneStateModule
    {
        public static AllRuneState GetRuneState(this IWorldState worldState,
            Address avatarAddress)
        {
            var serialized = worldState.GetResolvedState(avatarAddress, Addresses.RuneState);
            AllRuneState allRuneState;
            if (serialized is null)
            {
                // Get legacy rune states
                var runeListSheet = worldState.GetSheet<RuneListSheet>();
                allRuneState = new AllRuneState();
                foreach (var rune in runeListSheet.Values)
                {
                    var runeAddress = RuneState.DeriveAddress(avatarAddress, rune.Id);
                    if (worldState.TryGetLegacyState(runeAddress, out List rawState))
                    {
                        var runeState = new RuneState(rawState);
                        allRuneState.AddRuneState(runeState);
                    }
                }
            }
            else
            {
                allRuneState = new AllRuneState((List)serialized);
            }

            return allRuneState;
        }

        public static IWorld SetRuneState(this IWorld world, Address avatarAddress,
            AllRuneState allRuneState)
        {
            var account = world.GetAccount(Addresses.RuneState);
            account = account.SetState(avatarAddress, allRuneState.Serialize());
            return world.SetAccount(Addresses.RuneState, account);
        }
    }
}
