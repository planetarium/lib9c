using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Action.Extensions;
using Nekoyume.Model.State;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Module
{
    public static class AvatarModule
    {
        public static AvatarState GetAvatarState(IWorld world, Address address)
        {
            var account = AccountHelper.ResolveAccount(world, Addresses.Avatar);
            var serializedAvatar = account.GetState(address);
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

        public static AvatarState GetAvatarStateV2(IWorld world, Address address)
        {
            var account = AccountHelper.ResolveAccount(world, Addresses.Avatar);
            var addresses = new List<Address>
            {
                address,
            };
            string[] keys =
            {
                LegacyInventoryKey,
                LegacyWorldInformationKey,
                LegacyQuestListKey,
            };
            addresses.AddRange(keys.Select(key => AddressExtension.Derive(address, key)));
            var serializedValues = account.GetStates(addresses);
            if (!(serializedValues[0] is Dictionary serializedAvatar))
            {
                Log.Warning("No avatar state ({AvatarAddress})", address.ToHex());
                return null;
            }

            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var serializedValue = serializedValues[i + 1];
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

        public static AvatarState GetEnemyAvatarState(IWorld world, Address avatarAddress)
        {
            AvatarState enemyAvatarState;
            try
            {
                enemyAvatarState = GetAvatarStateV2(world, avatarAddress);
            }
            // BackWard compatible.
            catch (FailedLoadStateException)
            {
                enemyAvatarState = GetAvatarState(world, avatarAddress);
            }

            if (enemyAvatarState is null)
            {
                throw new FailedLoadStateException(
                    $"Aborted as the avatar state of the opponent ({avatarAddress}) was failed to load.");
            }

            return enemyAvatarState;
        }

        public static bool TryGetAvatarState(
            IWorld world,
            Address agentAddress,
            Address avatarAddress,
            out AvatarState avatarState
        )
        {
            var account = AccountHelper.ResolveAccount(world, Addresses.Avatar);
            avatarState = null;
            var value = account.GetState(avatarAddress);
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
            IWorld world,
            Address agentAddress,
            Address avatarAddress,
            out AvatarState avatarState,
            out bool migrationRequired
        )
        {
            var account = AccountHelper.ResolveAccount(world, Addresses.Avatar);
            avatarState = null;
            migrationRequired = false;
            if (account.GetState(avatarAddress) is Dictionary serializedAvatar)
            {
                try
                {
                    if (serializedAvatar[AgentAddressKey].ToAddress() != agentAddress)
                    {
                        return false;
                    }

                    avatarState = GetAvatarStateV2(world, avatarAddress);
                    return true;
                }
                catch (Exception e)
                {
                    // BackWardCompatible.
                    if (e is KeyNotFoundException || e is FailedLoadStateException)
                    {
                        migrationRequired = true;
                        return TryGetAvatarState(world, agentAddress, avatarAddress, out avatarState);
                    }

                    return false;
                }
            }

            return false;
        }

        // FIXME: Should not use this unified method.
        public static bool TryGetAgentAvatarStates(
            IWorld world,
            Address agentAddress,
            Address avatarAddress,
            out AgentState agentState,
            out AvatarState avatarState
        )
        {
            avatarState = null;
            agentState = AgentModule.GetAgentState(world, agentAddress);
            if (agentState is null)
            {
                return false;
            }

            if (!agentState.avatarAddresses.ContainsValue(avatarAddress))
            {
                throw new AgentStateNotContainsAvatarAddressException(
                    $"The avatar {avatarAddress.ToHex()} does not belong to the agent {agentAddress.ToHex()}.");
            }

            avatarState = GetAvatarState(world, avatarAddress);
            return !(avatarState is null);
        }

        // FIXME: Should not use this unified method.
        public static bool TryGetAgentAvatarStatesV2(
            IWorld world,
            Address agentAddress,
            Address avatarAddress,
            out AgentState agentState,
            out AvatarState avatarState,
            out bool avatarMigrationRequired
        )
        {
            avatarState = null;
            avatarMigrationRequired = false;
            agentState = AgentModule.GetAgentState(world, agentAddress);
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
                avatarState = GetAvatarStateV2(world, avatarAddress);
            }
            catch (FailedLoadStateException)
            {
                // BackWardCompatible.
                avatarState = GetAvatarState(world, avatarAddress);
                avatarMigrationRequired = true;
            }

            return !(avatarState is null);
        }

        public static IWorld SetAvatarState(IWorld world, Address address, AvatarState state)
        {
            // TODO: Override legacy address to null state?
            var account = world.GetAccount(Addresses.Avatar);
            account = account.SetState(address, state.Serialize());
            return world.SetAccount(account);
        }

        public static IWorld SetAvatarStateV2(IWorld world, Address address, AvatarState state)
        {
            // TODO: Override legacy address to null state?
            var account = world.GetAccount(Addresses.Avatar);
            account = account.SetState(address, state.SerializeV2());
            return world.SetAccount(account);
        }
    }
}
