using System;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Store.Trie;
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

        public static IWorld Ban(this IWorld world, GuildAddress guildAddress, Address agentAddress)
        {
            var accountAddress = Addresses.GetGuildBanAccountAddress(guildAddress);
            var account = world.GetAccount(accountAddress);
            account = account.SetState(agentAddress, (Boolean)true);
            return world.SetAccount(accountAddress, account);
        }

        public static IWorld Unban(this IWorld world, GuildAddress guildAddress, Address agentAddress)
        {
            var accountAddress = Addresses.GetGuildBanAccountAddress(guildAddress);
            var account = world.GetAccount(accountAddress);
            account = account.RemoveState(agentAddress);
            return world.SetAccount(accountAddress, account);
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
