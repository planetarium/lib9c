using System;
using Lib9c.Extensions;
using Lib9c.Model.Guild;
using Lib9c.TypedAddress;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Boolean = Bencodex.Types.Boolean;

namespace Lib9c.Module.Guild
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

        public static void Ban(
            this GuildRepository repository,
            AgentAddress signer,
            AgentAddress target)
        {
            if (repository.GetJoinedGuild(signer) is not { } guildAddress)
            {
                throw new InvalidOperationException("The signer does not have a guild.");
            }

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

            if (repository.GetJoinedGuild(target) == guildAddress)
            {
                repository.LeaveGuild(target);
            }

            repository.UpdateWorld(
                repository.World.MutateAccount(
                    Addresses.GetGuildBanAccountAddress(guildAddress),
                    account => account.SetState(target, (Boolean)true)));
        }

        public static void Unban(this GuildRepository repository, AgentAddress signer, Address target)
        {
            if (repository.GetJoinedGuild(signer) is not { } guildAddress)
            {
                throw new InvalidOperationException("The signer does not join any guild.");
            }

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
