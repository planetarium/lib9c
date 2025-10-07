#nullable enable
using System;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Extensions;
using Lib9c.Model.Guild;
using Lib9c.TypedAddress;

namespace Lib9c.Module.Guild
{
    public static class GuildMemberCounterModule
    {
        public static BigInteger GetGuildMemberCount(
            this GuildRepository repository, GuildAddress guildAddress)
        {
            var account = repository.World.GetAccountState(Addresses.GuildMemberCounter);
            return account.GetState(guildAddress) switch
            {
                Integer i => i.Value,
                null => 0,
                _ => throw new InvalidOperationException(),
            };
        }

        public static GuildRepository IncreaseGuildMemberCount(
            this GuildRepository repository, GuildAddress guildAddress)
        {
            repository.UpdateWorld(
                repository.World.MutateAccount(
                    Addresses.GuildMemberCounter, account =>
            {
                BigInteger count = account.GetState(guildAddress) switch
                {
                    Integer i => i.Value,
                    null => 0,
                    _ => throw new InvalidCastException(),
                };

                return account.SetState(guildAddress, (Integer)(count + 1));
            }));

            return repository;
        }

        public static GuildRepository DecreaseGuildMemberCount(
            this GuildRepository repository, GuildAddress guildAddress)
        {
            repository.UpdateWorld(
                repository.World.MutateAccount(
                    Addresses.GuildMemberCounter, account =>
            {
                BigInteger count = account.GetState(guildAddress) switch
                {
                    Integer i => i.Value,
                    null => 0,
                    _ => throw new InvalidCastException(),
                };

                if (count < 1)
                {
                    throw new InvalidOperationException(
                        "The number of guild member cannot be negative.");
                }

                return account.SetState(guildAddress, (Integer)(count - 1));
            }));

            return repository;
        }
    }
}
