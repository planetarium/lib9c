#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Delegation;
using Nekoyume.Extensions;
using Nekoyume.Model.Guild;
using Nekoyume.Module.Delegation;
using Nekoyume.TypedAddress;

namespace Nekoyume.Module.Guild
{
    public static class GuildParticipantModule
    {
        // Returns `null` when it didn't join any guild.
        // Returns `GuildAddress` when it joined a guild.
        public static GuildAddress? GetJoinedGuild(this IWorldState worldState, AgentAddress agentAddress)
        {
            return worldState.TryGetGuildParticipant(agentAddress, out var guildParticipant)
                ? guildParticipant.GuildAddress
                : null;
        }

        public static IWorld JoinGuildWithDelegate(
            this IWorld world,
            IActionContext context,
            GuildAddress guildAddress)
            => world
                .JoinGuild(guildAddress, new AgentAddress(context.Signer))
                .Delegate(context, guildAddress, world.GetBalance(context.Signer, Currencies.GuildGold));

        public static IWorld LeaveGuildWithUndelegate(
            this IWorld world,
            IActionContext context)
        {
            var guild = world.GetJoinedGuild(new AgentAddress(context.Signer)) is GuildAddress guildAddr
                ? world.GetGuild(guildAddr)
                : throw new InvalidOperationException("The signer does not join any guild.");

            return world
                .Undelegate(context, guildAddr, world.GetBond(guild, context.Signer).Share)
                .LeaveGuild(new AgentAddress(context.Signer));
        }

        public static IWorld MoveGuildWithRedelegate(
            this IWorld world,
            IActionContext context,
            GuildAddress srcGuildAddress,
            GuildAddress dstGuildAddress)
        {
            var agentAddress = new AgentAddress(context.Signer);
            var srcGuild = world.GetGuild(srcGuildAddress);
            return world
                .Redelegate(
                    context,
                    srcGuildAddress,
                    dstGuildAddress,
                    world.GetBond(srcGuild, context.Signer).Share)
                .LeaveGuild(agentAddress)
                .JoinGuild(dstGuildAddress, agentAddress);
        }

        public static IWorld JoinGuild(
            this IWorld world,
            GuildAddress guildAddress,
            AgentAddress target)
        {
            var guildParticipant = new Model.Guild.GuildParticipant(target, guildAddress);
            return world.MutateAccount(Addresses.GuildParticipant,
                    account => account.SetState(target, guildParticipant.Bencoded))
                .IncreaseGuildMemberCount(guildAddress);
        }

        public static IWorld LeaveGuild(
            this IWorld world,
            AgentAddress target)
        {
            if (world.GetJoinedGuild(target) is not { } guildAddress)
            {
                throw new InvalidOperationException("The signer does not join any guild.");
            }

            if (!world.TryGetGuild(guildAddress, out var guild))
            {
                throw new InvalidOperationException(
                    "There is no such guild.");
            }

            if (guild.GuildMasterAddress == target)
            {
                throw new InvalidOperationException(
                    "The signer is a guild master. Guild master cannot quit the guild.");
            }

            return RawLeaveGuild(world, target);
        }

        public static IWorld RawLeaveGuild(this IWorld world, AgentAddress target)
        {
            if (!world.TryGetGuildParticipant(target, out var guildParticipant))
            {
                throw new InvalidOperationException("It may not join any guild.");
            }

            return world.RemoveGuildParticipant(target)
                .DecreaseGuildMemberCount(guildParticipant.GuildAddress);
        }

        private static Model.Guild.GuildParticipant GetGuildParticipant(
            this IWorldState worldState, AgentAddress agentAddress)
        {
            var value = worldState.GetAccountState(Addresses.GuildParticipant)
                .GetState(agentAddress);
            if (value is List list)
            {
                return new Model.Guild.GuildParticipant(agentAddress, list);
            }

            throw new FailedLoadStateException("It may not join any guild.");
        }

        private static Model.Guild.GuildParticipant GetGuildParticipant(
            this IWorldState worldState,
            AgentAddress agentAddress,
            IDelegationRepository repository)
        {
            var value = worldState.GetAccountState(Addresses.GuildParticipant)
                .GetState(agentAddress);
            if (value is List list)
            {
                return new Model.Guild.GuildParticipant(agentAddress, list, repository);
            }

            throw new FailedLoadStateException("It may not join any guild.");
        }

        private static bool TryGetGuildParticipant(this IWorldState worldState,
            AgentAddress agentAddress,
            [NotNullWhen(true)] out Model.Guild.GuildParticipant? guildParticipant)
        {
            try
            {
                guildParticipant = GetGuildParticipant(worldState, agentAddress);
                return true;
            }
            catch
            {
                guildParticipant = null;
                return false;
            }
        }

        private static bool TryGetGuildParticipant(this IWorldState worldState,
            AgentAddress agentAddress,
            IDelegationRepository repository,
            [NotNullWhen(true)] out Model.Guild.GuildParticipant? guildParticipant)
        {
            try
            {
                guildParticipant = GetGuildParticipant(worldState, agentAddress, repository);
                return true;
            }
            catch
            {
                guildParticipant = null;
                return false;
            }
        }

        private static IWorld RemoveGuildParticipant(this IWorld world, AgentAddress agentAddress)
        {
            return world.MutateAccount(Addresses.GuildParticipant,
                account => account.RemoveState(agentAddress));
        }

        private static IWorld Delegate(
            this IWorld world,
            IActionContext context,
            GuildAddress guildAddress,
            FungibleAssetValue fav)
        {
            var repo = new DelegationRepository(world, context);

            var agentAddress = new AgentAddress(context.Signer);
            var guildParticipant = world.TryGetGuildParticipant(agentAddress, repo, out var p)
                ? p
                : throw new InvalidOperationException("The signer was not joined to any guild.");
            var guild = world.TryGetGuild(guildAddress, repo, out var g)
                ? g
                : throw new InvalidOperationException("The guild does not exist.");
            guildParticipant.Delegate(guild, fav, context.BlockIndex);

            return repo.World.SetGuild(guild).SetGuildParticipant(guildParticipant);
        }

        private static IWorld Undelegate(
            this IWorld world,
            IActionContext context,
            GuildAddress guildAddress,
            BigInteger share)
        {
            var repo = new DelegationRepository(world, context);

            var agentAddress = new AgentAddress(context.Signer);
            var guildParticipant = world.TryGetGuildParticipant(agentAddress, repo, out var p)
                ? p
                : throw new InvalidOperationException("The signer was not joined to any guild.");
            var guild = world.TryGetGuild(guildAddress, repo, out var g)
                ? g
                : throw new InvalidOperationException("The guild does not exist.");
            guildParticipant.Undelegate(guild, share, context.BlockIndex);

            return repo.World.SetGuild(guild).SetGuildParticipant(guildParticipant);
        }

        public static IWorld Redelegate(
            this IWorld world,
            IActionContext context,
            GuildAddress srcGuildAddress,
            GuildAddress dstGuildAddress,
            BigInteger share)
        {
            var repo = new DelegationRepository(world, context);

            var agentAddress = new AgentAddress(context.Signer);
            var guildParticipant = world.TryGetGuildParticipant(agentAddress, repo, out var p)
                ? p
                : throw new InvalidOperationException("The signer was not joined to any guild.");
            var srcGuild = world.TryGetGuild(srcGuildAddress, repo, out var s)
                ? s
                : throw new InvalidOperationException("The guild does not exist.");
            var dstGuild = world.TryGetGuild(srcGuildAddress, repo, out var d)
                ? d
                : throw new InvalidOperationException("The guild does not exist.");
            guildParticipant.Redelegate(srcGuild, dstGuild, share, context.BlockIndex);

            return repo.World.SetGuild(srcGuild).SetGuild(dstGuild).SetGuildParticipant(guildParticipant);
        }

        private static IWorld ClaimReward(
            this IWorld world,
            IActionContext context,
            GuildAddress guildAddress)
        {
            var repo = new DelegationRepository(world, context);

            var agentAddress = new AgentAddress(context.Signer);
            var guildParticipant = world.TryGetGuildParticipant(agentAddress, out var p)
                ? p
                : throw new InvalidOperationException("The signer was not joined to any guild.");
            var guild = world.TryGetGuild(guildAddress, out var g)
                ? g
                : throw new InvalidOperationException("The guild does not exist.");
            guildParticipant.ClaimReward(guild, context.BlockIndex);

            return repo.World.SetGuild(guild).SetGuildParticipant(guildParticipant);
        }

        private static IWorld SetGuildParticipant(
            this IWorld world,
            GuildParticipant guildParticipant)
            => world.MutateAccount(
                Addresses.GuildParticipant,
                account => account.SetState(guildParticipant.Address, guildParticipant.Bencoded));
    }
}
