#nullable enable
using System;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action.State;
using Nekoyume.Extensions;
using Nekoyume.TypedAddress;

namespace Nekoyume.Module.Guild
{
    public static class GuildMemberCounterModule
    {
        public static BigInteger GetGuildMemberCount(this IWorldState world, GuildAddress guildAddress)
        {
            var account = world.GetAccountState(Addresses.GuildMemberCounter);
            return account.GetState(guildAddress) switch
            {
                Integer i => i.Value,
                null => 0,
                _ => throw new InvalidOperationException(),
            };
        }

        public static IWorld IncreaseGuildMemberCount(this IWorld world, GuildAddress guildAddress)
        {
            return world.MutateAccount(Addresses.GuildMemberCounter, account =>
            {
                BigInteger count = account.GetState(guildAddress) switch
                {
                    Integer i => i.Value,
                    null => 0,
                    _ => throw new InvalidCastException(),
                };

                return account.SetState(guildAddress, (Integer)(count + 1));
            });
        }

        public static IWorld DecreaseGuildMemberCount(this IWorld world, GuildAddress guildAddress)
        {
            return world.MutateAccount(Addresses.GuildMemberCounter, account =>
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
            });
        }
    }
}
