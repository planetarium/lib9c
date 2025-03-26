using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Module
{
    public static class AvatarModule
    {
        // TODO change flags set default false
        public static AvatarState GetAvatarState(
            this IWorldState worldState,
            Address address,
            bool getInventory = true,
            bool getWorldInformation = true,
            bool getQuestList = true)
        {
            var account = worldState.GetAccountState(Addresses.Avatar);
            var serializedAvatarRaw = account.GetState(address);

            AvatarState avatarState = null;
            if (serializedAvatarRaw is List avatarList)
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
                catch (IndexOutOfRangeException e)
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
                    "List but: {Raw}",
                    address.ToHex(),
                    serializedAvatarRaw);
                return null;
            }

            try
            {
                if (getInventory)
                {
                    avatarState.inventory = GetInventoryV2(worldState, address);
                }

                if (getWorldInformation)
                {
                    avatarState.worldInformation = GetWorldInformationV2(worldState, address);
                }

                if (getQuestList)
                {
                    avatarState.questList = GetQuestListV2(worldState, address);
                }
            }
            catch (KeyNotFoundException)
            {
            }

            return avatarState;
        }

        // FIXME: Is this method required?
        public static AvatarState GetEnemyAvatarState(this IWorldState worldState, Address avatarAddress)
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
            this IWorldState worldState,
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
            this IWorld world,
            Address avatarAddress,
            AvatarState state,
            bool setAvatar = true,
            bool setInventory = true,
            bool setWorldInformation = true,
            bool setQuestList = true)
        {
            // TODO: Overwrite legacy address to null state?
            if (state.Version < 2)
            {
                // If the version of the avatar state is 0 or 1, overwrite flags to true.
                setAvatar = true;
                setInventory = true;
                setWorldInformation = true;
                setQuestList = true;
            }

            if (setAvatar)
            {
                world = SetAvatar(world, avatarAddress, state);
            }

            if (setInventory)
            {
                world = SetInventory(world, avatarAddress, state.inventory);
            }

            if (setWorldInformation)
            {
                world = SetWorldInformation(world, avatarAddress, state.worldInformation);
            }

            if (setQuestList)
            {
                world = SetQuestList(world, avatarAddress, state.questList);
            }

            return world;
        }

        private static IWorld SetAvatar(this IWorld world, Address address, AvatarState state)
        {
            var avatarAccount = world.GetAccount(Addresses.Avatar);
            avatarAccount = avatarAccount.SetState(address, state.SerializeList());
            return world.SetAccount(Addresses.Avatar, avatarAccount);
        }

        internal static IWorld SetCp(this IWorld world, Address address, BattleType battleType, int cp)
        {
            var cpAccount = world.GetAccount(Addresses.GetCpAccountAddress(battleType));
            cpAccount = cpAccount.SetState(address, new CpState(cp).Serialize());
            return world.SetAccount(Addresses.GetCpAccountAddress(battleType), cpAccount);
        }

        internal static Inventory GetInventoryV2(this IWorldState worldState, Address address)
        {
            var serializedInventory = worldState.GetAccountState(Addresses.Inventory).GetState(address);
            if (serializedInventory is null || serializedInventory.Equals(Null.Value))
            {
                throw new FailedLoadStateException(
                    $"Aborted as the inventory state of the avatar ({address}) was failed to load.");
            }

            return new Inventory((List)serializedInventory);
        }

        internal static IWorld SetInventory(this IWorld world, Address address, Inventory state)
        {
            var inventoryAccount = world.GetAccount(Addresses.Inventory);
            inventoryAccount = inventoryAccount.SetState(address, state.Serialize());
            return world.SetAccount(Addresses.Inventory, inventoryAccount);
        }

        internal static WorldInformation GetWorldInformationV2(this IWorldState worldState, Address address)
        {
            var serializeWorldInfo =
                worldState.GetAccountState(Addresses.WorldInformation).GetState(address);
            if (serializeWorldInfo is null || serializeWorldInfo.Equals(Null.Value))
            {
                throw new FailedLoadStateException(
                    $"Aborted as the worldInformation state of the avatar ({address}) was failed to load.");
            }

            return new WorldInformation((Dictionary)serializeWorldInfo);
        }

        internal static IWorld SetWorldInformation(this IWorld world, Address address, WorldInformation state)
        {
            var worldInfoAccount = world.GetAccount(Addresses.WorldInformation);
            worldInfoAccount = worldInfoAccount.SetState(address, state.Serialize());
            return world.SetAccount(Addresses.WorldInformation, worldInfoAccount);
        }

        private static QuestList GetQuestListV2(this IWorldState worldState, Address address)
        {
            var serializeQuestList = worldState.GetAccountState(Addresses.QuestList).GetState(address);
            if (serializeQuestList is null || serializeQuestList.Equals(Null.Value))
            {
                throw new FailedLoadStateException(
                    $"Aborted as the questList state of the avatar ({address}) was failed to load.");
            }

            return serializeQuestList switch
            {
                Dictionary d => new QuestList(d),
                List l => new QuestList(l),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IWorld SetQuestList(this IWorld world, Address address, QuestList state)
        {
            var questListAccount = world.GetAccount(Addresses.QuestList);
            questListAccount = questListAccount.SetState(address, state.Serialize());
            return world.SetAccount(Addresses.QuestList, questListAccount);
        }

    }
}
