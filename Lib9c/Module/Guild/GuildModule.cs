#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Bencodex.Types;
using Libplanet.Action.State;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.TypedAddress;

namespace Nekoyume.Module.Guild
{
    public static class GuildModule
    {
        public static Model.Guild.Guild GetGuild(this IWorldState worldState, GuildAddress guildAddress)
        {
            var value = worldState.GetAccountState(Addresses.Guild).GetState(guildAddress);
            if (value is List list)
            {
                return new Model.Guild.Guild(list, worldState.GetGoldCurrency());
            }

            throw new FailedLoadStateException("There is no such guild.");
        }

        public static bool TryGetGuild(this IWorldState worldState,
            GuildAddress guildAddress, [NotNullWhen(true)] out Model.Guild.Guild? guild)
        {
            try
            {
                guild = GetGuild(worldState, guildAddress);
                return true;
            }
            catch
            {
                guild = null;
                return false;
            }
        }

        public static IWorld MakeGuild(this IWorld world, GuildAddress guildAddress, AgentAddress signer)
        {
            if (world.GetJoinedGuild(signer) is not null)
            {
                throw new InvalidOperationException("The signer already has a guild.");
            }

            if (world.TryGetGuild(guildAddress, out _))
            {
                throw new InvalidOperationException("Duplicated guild address. Please retry.");
            }

            return world.MutateAccount(Addresses.Guild,
                account =>
                    account.SetState(guildAddress,
                        new Model.Guild.Guild(signer, world.GetGoldCurrency()).Bencoded))
                .JoinGuild(guildAddress, signer);
        }

        public static IWorld RemoveGuild(this IWorld world, AgentAddress signer)
        {
            if (world.GetJoinedGuild(signer) is not { } guildAddress)
            {
                throw new InvalidOperationException("The signer does not join any guild.");
            }

            if (!world.TryGetGuild(guildAddress, out var guild))
            {
                throw new InvalidOperationException("There is no such guild.");
            }

            if (guild.GuildMasterAddress != signer)
            {
                throw new InvalidOperationException("The signer is not a guild master.");
            }

            if (world.GetGuildMemberCount(guildAddress) > 1)
            {
                throw new InvalidOperationException("There are remained participants in the guild.");
            }

            return world
                .RawLeaveGuild(signer)
                .MutateAccount(Addresses.Guild, account => account.RemoveState(guildAddress))
                .RemoveBanList(guildAddress);
        }

        public static IWorld SetGuild(this IWorld world, Model.Guild.Guild guild)
            => world.MutateAccount(
                Addresses.Guild,
                account => account.SetState(guild.Address, guild.Bencoded));
    }
}
