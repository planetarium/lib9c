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
            AllRuneState allRuneState;
            var subStart1 = DateTimeOffset.UtcNow;
            if (serialized is null)
            {
                // Get legacy rune states
                subStart = DateTimeOffset.UtcNow;
                var runeListSheet = worldState.GetSheet<RuneListSheet>();
                subEnd = DateTimeOffset.UtcNow;
                Log.Debug(
                    "[DataProvider] AvatarInfo RuneStateModule3 Address: {0} Time Taken: {1} ms.",
                    avatarAddress,
                    (subEnd - subStart).Milliseconds);
                subStart = DateTimeOffset.UtcNow;
                allRuneState = new AllRuneState();
                subEnd = DateTimeOffset.UtcNow;
                Log.Debug(
                    "[DataProvider] AvatarInfo RuneStateModule4 Address: {0} Time Taken: {1} ms.",
                    avatarAddress,
                    (subEnd - subStart).Milliseconds);
                var subSubStart = DateTimeOffset.UtcNow;
                foreach (var rune in runeListSheet.Values)
                {
                    subStart = DateTimeOffset.UtcNow;
                    var runeAddress = RuneState.DeriveAddress(avatarAddress, rune.Id);
                    subEnd = DateTimeOffset.UtcNow;
                    Log.Debug(
                        "[DataProvider] AvatarInfo RuneStateModule5 Address: {0} Time Taken: {1} ms.",
                        avatarAddress,
                        (subEnd - subStart).Milliseconds);
                    subStart = DateTimeOffset.UtcNow;
                    if (worldState.TryGetLegacyState(runeAddress, out List rawState))
                    {
                        subEnd = DateTimeOffset.UtcNow;
                        Log.Debug(
                            "[DataProvider] AvatarInfo RuneStateModule6 Address: {0} Time Taken: {1} ms.",
                            avatarAddress,
                            (subEnd - subStart).Milliseconds);
                        subStart = DateTimeOffset.UtcNow;
                        var runeState = new RuneState(rawState);
                        subEnd = DateTimeOffset.UtcNow;
                        Log.Debug(
                            "[DataProvider] AvatarInfo RuneStateModule7 Address: {0} Time Taken: {1} ms.",
                            avatarAddress,
                            (subEnd - subStart).Milliseconds);
                        subStart = DateTimeOffset.UtcNow;
                        allRuneState.AddRuneState(runeState);
                        subEnd = DateTimeOffset.UtcNow;
                        Log.Debug(
                            "[DataProvider] AvatarInfo RuneStateModule8 Address: {0} Time Taken: {1} ms.",
                            avatarAddress,
                            (subEnd - subStart).Milliseconds);
                    }
                }

                var subSubEnd = DateTimeOffset.UtcNow;
                Log.Debug(
                    "[DataProvider] AvatarInfo RuneStateModule9 Address: {0} Time Taken: {1} ms.",
                    avatarAddress,
                    (subSubEnd - subSubStart).Milliseconds);

                migrateRequired = true;
            }
            else
            {
                subStart = DateTimeOffset.UtcNow;
                allRuneState = new AllRuneState((List)serialized);
                subEnd = DateTimeOffset.UtcNow;
                Log.Debug(
                    "[DataProvider] AvatarInfo RuneStateModule10 Address: {0} Time Taken: {1} ms.",
                    avatarAddress,
                    (subEnd - subStart).Milliseconds);
            }

            var subEnd1 = DateTimeOffset.UtcNow;
            Log.Debug(
                "[DataProvider] AvatarInfo RuneStateModule11 Address: {0} Time Taken: {1} ms.",
                avatarAddress,
                (subEnd1 - subStart1).Milliseconds);
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
