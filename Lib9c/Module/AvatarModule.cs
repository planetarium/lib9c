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
        public static AvatarState GetAvatarState(IWorldState worldState, Address address)
        {
            var serializedAvatar = AccountHelper.Resolve(worldState, address, Addresses.Avatar);
            if (serializedAvatar is null)
            {
                Log.Warning("No avatar state ({AvatarAddress})", address.ToHex());
                return null;
            }

            try
            {
                return new AvatarState((Bencodex.Types.Dictionary)serializedAvatar);
            }
            catch (InvalidCastException e)
            {
                Log.Error(
                    e,
                    "Invalid avatar state ({AvatarAddress}): {SerializedAvatar}",
                    address.ToHex(),
                    serializedAvatar
                );

                return null;
            }
        }

        public static AvatarState GetAvatarStateV2(IWorldState worldState, Address address)
        {
            string[] keys =
            {
                LegacyInventoryKey,
                LegacyWorldInformationKey,
                LegacyQuestListKey,
            };
            var addresses = keys.Select(key => address.Derive(key)).ToArray();
            var serializedAvatarRaw = AccountHelper.Resolve(worldState, address, Addresses.Avatar);
            var serializedValues = LegacyModule.GetStates(worldState, addresses);
            if (!(serializedAvatarRaw is Dictionary serializedAvatar))
            {
                Log.Warning("No avatar state ({AvatarAddress})", address.ToHex());
                return null;
            }

            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var serializedValue = serializedValues[i];
                if (serializedValue is null)
                {
                    throw new FailedLoadStateException($"failed to load {key}.");
                }

                serializedAvatar = serializedAvatar.SetItem(key, serializedValue);
            }

            try
            {
                return new AvatarState(serializedAvatar);
            }
            catch (InvalidCastException e)
            {
                Log.Error(
                    e,
                    "Invalid avatar state ({AvatarAddress}): {SerializedAvatar}",
                    address.ToHex(),
                    serializedAvatar
                );

                return null;
            }
        }

        public static AvatarState GetEnemyAvatarState(IWorldState worldState, Address avatarAddress)
        {
            AvatarState enemyAvatarState;
            try
            {
                enemyAvatarState = GetAvatarStateV2(worldState, avatarAddress);
            }
            // BackWard compatible.
            catch (FailedLoadStateException)
            {
                enemyAvatarState = GetAvatarState(worldState, avatarAddress);
            }

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
            var value = AccountHelper.Resolve(worldState, avatarAddress, Addresses.Avatar);
            if (value is null)
            {
                return false;
            }

            try
            {
                var serializedAvatar = (Dictionary)value;
                if (serializedAvatar["agentAddress"].ToAddress() != agentAddress)
                {
                    return false;
                }

                avatarState = new AvatarState(serializedAvatar);
                return true;
            }
            catch (InvalidCastException)
            {
                return false;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        public static bool TryGetAvatarStateV2(
            IWorldState worldState,
            Address agentAddress,
            Address avatarAddress,
            out AvatarState avatarState,
            out bool migrationRequired
        )
        {
            avatarState = null;
            migrationRequired = false;
            if (AccountHelper.Resolve(worldState, avatarAddress, Addresses.Avatar) is Dictionary
                serializedAvatar)
            {
                try
                {
                    if (serializedAvatar[AgentAddressKey].ToAddress() != agentAddress)
                    {
                        return false;
                    }

                    avatarState = GetAvatarStateV2(worldState, avatarAddress);
                    return true;
                }
                catch (Exception e)
                {
                    // BackWardCompatible.
                    if (e is KeyNotFoundException || e is FailedLoadStateException)
                    {
                        migrationRequired = true;
                        return TryGetAvatarState(
                            worldState,
                            agentAddress,
                            avatarAddress,
                            out avatarState);
                    }

                    return false;
                }
            }

            return false;
        }

        // FIXME: Should not use this unified method.
        public static bool TryGetAgentAvatarStates(
            IWorldState worldState,
            Address agentAddress,
            Address avatarAddress,
            out AgentState agentState,
            out AvatarState avatarState
        )
        {
            avatarState = null;
            agentState = AgentModule.GetAgentState(worldState, agentAddress);
            if (agentState is null)
            {
                return false;
            }

            if (!agentState.avatarAddresses.ContainsValue(avatarAddress))
            {
                throw new AgentStateNotContainsAvatarAddressException(
                    $"The avatar {avatarAddress.ToHex()} does not belong to the agent {agentAddress.ToHex()}.");
            }

            avatarState = GetAvatarState(worldState, avatarAddress);
            return !(avatarState is null);
        }

        // FIXME: Should not use this unified method.
        public static bool TryGetAgentAvatarStatesV2(
            IWorldState worldState,
            Address agentAddress,
            Address avatarAddress,
            out AgentState agentState,
            out AvatarState avatarState,
            out bool avatarMigrationRequired
        )
        {
            avatarState = null;
            avatarMigrationRequired = false;
            agentState = AgentModule.GetAgentState(worldState, agentAddress);
            if (agentState is null)
            {
                return false;
            }

            if (!agentState.avatarAddresses.ContainsValue(avatarAddress))
            {
                throw new AgentStateNotContainsAvatarAddressException(
                    $"The avatar {avatarAddress.ToHex()} does not belong to the agent {agentAddress.ToHex()}.");
            }

            try
            {
                avatarState = GetAvatarStateV2(worldState, avatarAddress);
            }
            catch (FailedLoadStateException)
            {
                // BackWardCompatible.
                avatarState = GetAvatarState(worldState, avatarAddress);
                avatarMigrationRequired = true;
            }

            return !(avatarState is null);
        }

        public static IWorld SetAvatarState(IWorld world, Address address, AvatarState state)
        {
            // TODO: Override legacy address to null state?
            var account = world.GetAccount(Addresses.Avatar);
            account = account.SetState(address, state.Serialize());
            return world.SetAccount(Addresses.Avatar, account);
        }

        public static IWorld SetAvatarStateV2(
            IWorld world,
            Address avatarAddress,
            AvatarState state,
            bool setAvatar = true,
            bool setInventory = true,
            bool setWorldInformation = true,
            bool setQuestList = true)
        {
            if (setAvatar)
            {
                world = SetAvatarV2(world, avatarAddress, state);
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

        public static IWorld SetAvatarV2(IWorld world, Address address, AvatarState state)
        {
            var avatarAccount = world.GetAccount(Addresses.Avatar);
            avatarAccount = avatarAccount.SetState(address, state.SerializeV2());
            return world.SetAccount(Addresses.Avatar, avatarAccount);
        }

        public static IWorld MarkChanged(IWorld world, Address address) =>
            world.SetAccount(
                Addresses.Avatar,
                world.GetAccount(Addresses.Avatar).SetState(
                    address, ActionBase.MarkChanged));

        public static bool Changed(IWorld world, Address address) =>
            world.GetAccount(Addresses.Avatar).GetState(address).Equals(ActionBase.MarkChanged);

        public static IWorld SetInventory(IWorld world, Address address, Inventory state)
        {
            var legacyAccount = world.GetAccount(ReservedAddresses.LegacyAccount);
            legacyAccount = legacyAccount.SetState(address, state.Serialize());
            return world.SetAccount(ReservedAddresses.LegacyAccount, legacyAccount);
        }

        public static IWorld SetWorldInformation(IWorld world, Address address, WorldInformation state)
        {
            var legacyAccount = world.GetAccount(ReservedAddresses.LegacyAccount);
            legacyAccount = legacyAccount.SetState(address, state.Serialize());
            return world.SetAccount(ReservedAddresses.LegacyAccount, legacyAccount);
        }

        public static IWorld SetQuestList(IWorld world, Address address, QuestList state)
        {
            var legacyAccount = world.GetAccount(ReservedAddresses.LegacyAccount);
            legacyAccount = legacyAccount.SetState(address, state.Serialize());
            return world.SetAccount(ReservedAddresses.LegacyAccount, legacyAccount);
        }

    }
}
