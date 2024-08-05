using System;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Store.Trie;
using Nekoyume.Extensions;
using Nekoyume.TypedAddress;
using Boolean = Bencodex.Types.Boolean;

namespace Nekoyume.Module.Guild
{
    public static class GuildBanModule
    {
        public static bool IsBanned(this IWorldState worldState, GuildAddress guildAddress, Address agentAddress)
        {
            var accountAddress = Addresses.GetGuildBanAccountAddress(guildAddress);
            var value = worldState.GetAccountState(accountAddress)
                .GetState(agentAddress);
            if (value is Boolean boolean)
            {
                if (!boolean)
                {
                    throw new InvalidOperationException();
                }

                return true;
            }

            return false;
        }

        public static IWorld Ban(this IWorld world, GuildAddress guildAddress, AgentAddress signer, AgentAddress target)
        {
            if (!world.TryGetGuild(guildAddress, out var guild))
            {
                throw new InvalidOperationException("There is no such guild.");
            }

            if (guild.GuildMasterAddress != signer)
            {
                throw new InvalidOperationException("The signer is not a guild master.");
            }

            if (guild.GuildMasterAddress == target)
            {
                throw new InvalidOperationException("The guild master cannot be banned.");
            }

            if (world.TryGetGuildApplication(target, out var guildApplication) && guildApplication.GuildAddress == guildAddress)
            {
                world = world.RejectGuildApplication(signer, target);
            }

            if (world.GetJoinedGuild(target) == guildAddress)
            {
                world = world.LeaveGuild(target);
            }

            return world.MutateAccount(Addresses.GetGuildBanAccountAddress(guildAddress), account => account.SetState(target, (Boolean)true));
        }

        public static IWorld Unban(this IWorld world, GuildAddress guildAddress, AgentAddress signer, Address target)
        {
            if (!world.TryGetGuild(guildAddress, out var guild))
            {
                throw new InvalidOperationException("There is no such guild.");
            }

            if (guild.GuildMasterAddress != signer)
            {
                throw new InvalidOperationException("The signer is not a guild master.");
            }

            if (!world.IsBanned(guildAddress, target))
            {
                throw new InvalidOperationException("The target is not banned.");
            }

            return world.MutateAccount(Addresses.GetGuildBanAccountAddress(guildAddress),
                account => account.RemoveState(target));
        }

        public static IWorld RemoveBanList(this IWorld world, GuildAddress guildAddress) =>
            world.SetAccount(Addresses.GetGuildBanAccountAddress(guildAddress), GetEmptyAccount(world));

        private static IAccount GetEmptyAccount(this IWorld world)
        {
            var account = world.GetAccount(Addresses.EmptyAccountAddress);
            if (account.Trie.Root is not null)
            {
                throw new InvalidOperationException(
                    $"The {Addresses.EmptyAccountAddress.ToString()} account must be empty.");
            }

            return account;
        }
    }
}
