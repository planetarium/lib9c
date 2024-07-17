#nullable enable
using System;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using Nekoyume.TableData.Rune;
using Serilog;

namespace Nekoyume.Module
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
            var subStart = DateTimeOffset.UtcNow;
            var account = worldState.GetAccountState(Addresses.RuneState);
            var subEnd = DateTimeOffset.UtcNow;
            Log.Debug(
                "[DataProvider] AvatarInfo RuneStateModule1 Address: {0} Time Taken: {1} ms.",
                avatarAddress,
                (subEnd - subStart).Milliseconds);
            subStart = DateTimeOffset.UtcNow;
            var serialized = account.GetState(avatarAddress);
            subEnd = DateTimeOffset.UtcNow;
            Log.Debug(
                "[DataProvider] AvatarInfo RuneStateModule2 Address: {0} Time Taken: {1} ms.",
                avatarAddress,
                (subEnd - subStart).Milliseconds);
            subStart = DateTimeOffset.UtcNow;
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

            subEnd = DateTimeOffset.UtcNow;
            Log.Debug(
                "[DataProvider] AvatarInfo RuneStateModule3 Address: {0} Time Taken: {1} ms.",
                avatarAddress,
                (subEnd - subStart).Milliseconds);
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
