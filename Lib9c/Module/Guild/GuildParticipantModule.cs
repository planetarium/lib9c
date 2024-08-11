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

        // TODO: Implement `MoveGuild()`, `MoveGuildWithRedelegate()`, `Redelegate()` method.

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

        private static Model.Guild.GuildParticipant GetGuildParticipant(this IWorldState worldState, AgentAddress agentAddress)
        {
            var value = worldState.GetAccountState(Addresses.GuildParticipant)
                .GetState(agentAddress);
            if (value is List list)
            {
                return new Model.Guild.GuildParticipant(agentAddress, list);
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
            world = world.ClaimReward(context, guildAddress);

            var agentAddress = new AgentAddress(context.Signer);
            var guildParticipant = world.TryGetGuildParticipant(agentAddress, out var p)
                ? p
                : new GuildParticipant(agentAddress, guildAddress);
            var guild = world.TryGetGuild(guildAddress, out var g)
                ? g
                : throw new InvalidOperationException("The guild does not exist.");
            var bond = world.GetBond(guild, agentAddress);
            var result = guildParticipant.Delegate(guild, fav, context.BlockIndex, bond);

            return world
                .SetBond(result.Bond)
                .SetGuild(result.Delegatee)
                .SetGuildParticipant(guildParticipant)
                .TransferAsset(context, agentAddress, guildAddress, result.DelegatedFAV);
        }

        private static IWorld Undelegate(
            this IWorld world,
            IActionContext context,
            GuildAddress guildAddress,
            BigInteger share)
        {
            world = world.ClaimReward(context, guildAddress);

            var agentAddress = new AgentAddress(context.Signer);
            var guildParticipant = world.TryGetGuildParticipant(agentAddress, out var p)
                ? p
                : new GuildParticipant(agentAddress, guildAddress);
            var guild = world.TryGetGuild(guildAddress, out var g)
                ? g
                : throw new InvalidOperationException("The guild does not exist.");
            var bond = world.GetBond(guild, agentAddress);
            var unbondLockIn = world.GetUnbondLockIn(guild, agentAddress);
            var unbondingSet = world.GetUnbondingSet();
            var result = guildParticipant.Undelegate(
                guild, share, context.BlockIndex, bond, unbondLockIn, unbondingSet);

            return world
                .SetBond(result.Bond)
                .SetGuild(result.Delegatee)
                .SetGuildParticipant(guildParticipant)
                .SetUnbondLockIn(result.UnbondLockIn)
                .SetUnbondingSet(result.UnbondingSet);
        }

        private static IWorld ClaimReward(
            this IWorld world,
            IActionContext context,
            GuildAddress guildAddress)
        {
            var agentAddress = new AgentAddress(context.Signer);
            var guildParticipant = world.TryGetGuildParticipant(agentAddress, out var p)
                ? p
                : new GuildParticipant(agentAddress, guildAddress);
            var guild = world.TryGetGuild(guildAddress, out var g)
                ? g
                : throw new InvalidOperationException("The guild does not exist.");
            var bond = world.GetBond(guild, agentAddress);
            var rewardRecords = world.GetLumpSumRewardsRecords(
                guild, context.BlockIndex, guildParticipant.LastRewardHeight);

            var claimRewardResult = guildParticipant.ClaimReward(
                guild, rewardRecords, bond, context.BlockIndex);

            return world
                .SetLumpSumRewardsRecord(claimRewardResult.LumpSumRewardsRecord)
                .SetGuild(claimRewardResult.Delegatee)
                .SetGuildParticipant(guildParticipant)
                .TransferAsset(context, guild.RewardPoolAddress, agentAddress, claimRewardResult.Reward);
        }

        private static IWorld SetGuildParticipant(
            this IWorld world,
            GuildParticipant guildParticipant)
            => world.MutateAccount(
                Addresses.GuildParticipant,
                account => account.SetState(guildParticipant.Address, guildParticipant.Bencoded));
    }
}
