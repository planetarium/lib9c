using System;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Model.Guild;
using Nekoyume.TypedAddress;
using Boolean = Bencodex.Types.Boolean;

namespace Nekoyume.Module.Guild
{
    public static class GuildBanModule
    {
        public static bool IsBanned(this GuildRepository repository, GuildAddress guildAddress, Address agentAddress)
        {
            var accountAddress = Addresses.GetGuildBanAccountAddress(guildAddress);
            var value = repository.World.GetAccountState(accountAddress)
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

        public static void Ban(this GuildRepository repository, GuildAddress guildAddress, AgentAddress signer, AgentAddress target)
        {
            if (!repository.TryGetGuild(guildAddress, out var guild))
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

            if (repository.TryGetGuildApplication(target, out var guildApplication) && guildApplication.GuildAddress == guildAddress)
            {
                repository.RejectGuildApplication(signer, target);
            }

            if (repository.GetJoinedGuild(target) == guildAddress)
            {
                repository.LeaveGuild(target);
            }

            repository.UpdateWorld(
                repository.World.MutateAccount(
                    Addresses.GetGuildBanAccountAddress(guildAddress),
                    account => account.SetState(target, (Boolean)true)));
        }

        public static void Unban(this GuildRepository repository, GuildAddress guildAddress, AgentAddress signer, Address target)
        {
            if (!repository.TryGetGuild(guildAddress, out var guild))
            {
                throw new InvalidOperationException("There is no such guild.");
            }

            if (guild.GuildMasterAddress != signer)
            {
                throw new InvalidOperationException("The signer is not a guild master.");
            }

            if (!repository.IsBanned(guildAddress, target))
            {
                throw new InvalidOperationException("The target is not banned.");
            }

            repository.UpdateWorld(
                repository.World.MutateAccount(
                    Addresses.GetGuildBanAccountAddress(guildAddress),
                    account => account.RemoveState(target)));
        }

        public static void RemoveBanList(this GuildRepository repository, GuildAddress guildAddress) =>
            repository.UpdateWorld(
                repository.World.SetAccount(
                    Addresses.GetGuildBanAccountAddress(guildAddress),
                    GetEmptyAccount(repository.World)));

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
