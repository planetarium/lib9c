#nullable enable
using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Model.Guild;
using Nekoyume.TypedAddress;

namespace Nekoyume.Module.Guild
{
    public static class GuildRewardPoolModule
    {
        public static IWorld AddToGuildRewardPool(this IWorld world, GuildAddress guildAddress, AgentAddress agentAddress)
        {
            return world.MutateAccount(Addresses.GuildRewardPool, account =>
            {
                var set = account.GetState(guildAddress) switch
                {
                    List list => new GuildRewardPool(list),
                    null => new GuildRewardPool(ImmutableHashSet<Address>.Empty),
                    _ => throw new InvalidCastException(),
                };

                return account.SetState(guildAddress, set.Add(agentAddress).Bencoded);
            });
        }

        public static IWorld RemoveFromGuildRewardPool(this IWorld world, GuildAddress guildAddress, AgentAddress agentAddress)
        {
            return world.MutateAccount(Addresses.GuildRewardPool, account =>
            {
                GuildRewardPool? set = account.GetState(guildAddress) switch
                {
                    List list => new GuildRewardPool(list),
                    null => null,
                    _ => throw new InvalidCastException(),
                };

                return set is null
                    ? account
                    : account.SetState(guildAddress, set.Remove(agentAddress).Bencoded);
            });
        }
    }
}
