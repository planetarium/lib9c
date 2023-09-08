using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Action.Extensions;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Module
{
    public static class AvatarModule
    {
        // This method automatically determines if given IValue is a legacy avatar state or not.
        public static AvatarState GetAvatarState(IWorldState worldState, Address address)
        {
            var serializedAvatarRaw = AccountHelper.Resolve(worldState, address, Addresses.Avatar);

            AvatarState avatarState = null;
            if (serializedAvatarRaw is Dictionary avatarDict)
            {
                try
                {
                    avatarState = new AvatarState(avatarDict);
                }
                catch (InvalidCastException e)
                {
                    Log.Error(
                        e,
                        "Invalid avatar state ({AvatarAddress}): {SerializedAvatar}",
                        address.ToHex(),
                        avatarDict
                    );

                    return null;
                }
            }
            else if (serializedAvatarRaw is List avatarList)
            {
                try
                {
                    avatarState = new AvatarState(avatarList);
                }
                catch (InvalidCastException e)
                {
                    Log.Error(
                        e,
                        "Invalid avatar state ({AvatarAddress}): {SerializedAvatar}",
                        address.ToHex(),
                        avatarList
                    );

                    return null;
                }
            }
            else
            {
                Log.Warning(
                    "Avatar state ({AvatarAddress}) should be " +
                    "Dictionary or List but: {Raw}",
                    address.ToHex(),
                    serializedAvatarRaw);
                return null;
            }

            try
            {
                string[] keys =
                {
                    LegacyInventoryKey,
                    LegacyWorldInformationKey,
                    LegacyQuestListKey,
                };
                var addresses = keys.Select(key => address.Derive(key)).ToArray();
                var serializedValues = LegacyModule.GetStates(worldState, addresses);

                // Version 0 contains inventory, worldInformation, questList itself.
                if (avatarState.Version > 0)
                {
                    for (var i = 0; i < keys.Length; i++)
                    {
                        if (serializedValues[i] is null)
                        {
                            throw new FailedLoadStateException(
                                $"failed to load {keys[i]}.");
                        }
                    }

                    avatarState.inventory = new Inventory((List)serializedValues[0]);
                    avatarState.worldInformation =
                        new WorldInformation((Dictionary)serializedValues[1]);
                    avatarState.questList = new QuestList((Dictionary)serializedValues[2]);
                }
            }
            catch (KeyNotFoundException)
            {
            }

            return avatarState;
        }

        // FIXME: Is this method required?
        public static AvatarState GetEnemyAvatarState(IWorldState worldState, Address avatarAddress)
        {
            AvatarState enemyAvatarState = GetAvatarState(worldState, avatarAddress);

            if (enemyAvatarState is null)
            {
                throw new FailedLoadStateException(
                    $"Aborted as the avatar state of the opponent ({avatarAddress}) was failed to load.");
            }

            return enemyAvatarState;
        }

        public static bool TryGetAvatarState(
            IWorldState worldState,
            Address agentAddress,
            Address avatarAddress,
            out AvatarState avatarState
        )
        {
            avatarState = null;
            try
            {
                var temp = GetAvatarState(worldState, avatarAddress);
                if (temp is null || !temp.agentAddress.Equals(agentAddress))
                {
                    return false;
                }

                avatarState = temp;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static IWorld SetAvatarState(
            IWorld world,
            Address avatarAddress,
            AvatarState state,
            bool setAvatar,
            bool setInventory,
            bool setWorldInformation,
            bool setQuestList)
        {
            if (state.Version == 0)
            {
                // If the version of the avatar state is 0, overwrite flags to true.
                setAvatar = true;
                setInventory = true;
                setWorldInformation = true;
            }

            if (setAvatar)
            {
                world = SetAvatar(world, avatarAddress, state);
            }

            if (setInventory)
            {
                world = SetInventory(world, avatarAddress.Derive(LegacyInventoryKey), state.inventory);
            }

            if (setWorldInformation)
            {
                world = SetWorldInformation(world, avatarAddress.Derive(LegacyWorldInformationKey), state.worldInformation);
            }

            if (setQuestList)
            {
                world = SetQuestList(world, avatarAddress.Derive(LegacyQuestListKey), state.questList);
            }

            return world;
        }

        public static IWorld MarkChanged(IWorld world, Address address) =>
            world.SetAccount(
                world.GetAccount(Addresses.Avatar).SetState(
                    address, ActionBase.MarkChanged));

        public static bool Changed(IWorld world, Address address) =>
            world.GetAccount(Addresses.Avatar).GetState(address).Equals(ActionBase.MarkChanged);

        private static IWorld SetAvatar(IWorld world, Address address, AvatarState state)
        {
            var avatarAccount = world.GetAccount(Addresses.Avatar);
            avatarAccount = avatarAccount.SetState(address, state.SerializeList());
            return world.SetAccount(avatarAccount);
        }

        private static IWorld SetInventory(IWorld world, Address address, Inventory state)
        {
            var legacyAccount = world.GetAccount(ReservedAddresses.LegacyAccount);
            legacyAccount = legacyAccount.SetState(address, state.Serialize());
            return world.SetAccount(legacyAccount);
        }

        private static IWorld SetWorldInformation(IWorld world, Address address, WorldInformation state)
        {
            var legacyAccount = world.GetAccount(ReservedAddresses.LegacyAccount);
            legacyAccount = legacyAccount.SetState(address, state.Serialize());
            return world.SetAccount(legacyAccount);
        }

        private static IWorld SetQuestList(IWorld world, Address address, QuestList state)
        {
            var legacyAccount = world.GetAccount(ReservedAddresses.LegacyAccount);
            legacyAccount = legacyAccount.SetState(address, state.Serialize());
            return world.SetAccount(legacyAccount);
        }

    }
}
