#nullable enable
using Bencodex.Types;
using Lib9c.Model.State;
using Lib9c.TableData.Rune;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Module
{
    /// <summary>
    /// RuneStateModule is the module to use RuneState/AllRuneState with account.
    /// </summary>
    public static class RuneStateModule
    {
        public static AllRuneState GetRuneState(this IWorldState worldState,
            Address avatarAddress, out bool migrateRequired)
        {
            migrateRequired = false;
            var account = worldState.GetAccountState(Addresses.RuneState);
            var serialized = account.GetState(avatarAddress);
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

                migrateRequired = true;
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
