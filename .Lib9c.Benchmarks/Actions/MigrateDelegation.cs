using BenchmarkDotNet.Attributes;
using Bencodex.Types;
using Lib9c.Tests.Action;
using Lib9c.Tests.Util;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Nekoyume;
using Nekoyume.Action.Guild;
using Nekoyume.Action.Guild.Migration;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Extensions;
using Nekoyume.TypedAddress;

namespace Lib9c.Benchmarks.Actions;

public class MigrateDelegation
{
    private GuildAddress planetariumGuild = AddressUtil.CreateGuildAddress();
    private AgentAddress target = AddressUtil.CreateAgentAddress();
    private AgentAddress signer = AddressUtil.CreateAgentAddress();
    private IWorld worldEmpty;
    private IWorld worldBeforeGuildMigration;
    private IWorld worldBeforeParticipantMigration;
    private IWorld worldAfterMigration;

    [GlobalSetup]
    public void Setup()
    {
        worldEmpty = new World(MockUtil.MockModernWorldState);
        var legacyPlanetariumGuild = new LegacyGuild(GuildConfig.PlanetariumGuildOwner);
        var legacyPlanetariumGuildParticipant = new LegacyGuildParticipant(planetariumGuild);
        worldBeforeGuildMigration = worldEmpty
            .MutateAccount(
                Addresses.Guild,
                account => account.SetState(planetariumGuild, legacyPlanetariumGuild.Bencoded))
            .MutateAccount(
                Addresses.GuildParticipant,
                account => account.SetState(GuildConfig.PlanetariumGuildOwner, legacyPlanetariumGuildParticipant.Bencoded))
            .MutateAccount(
                Addresses.GuildParticipant,
                account => account.SetState(target, legacyPlanetariumGuildParticipant.Bencoded))
            .MutateAccount(
                Addresses.GuildMemberCounter,
                account => account.SetState(planetariumGuild, (Integer)2));
        worldBeforeParticipantMigration = new MigratePlanetariumGuild().Execute(new ActionContext
        {
            PreviousState = worldBeforeGuildMigration,
            Signer = new PrivateKey().Address,
        });
    }

    [Benchmark]
    public void Execute_Empty()
    {
        var action = new Nekoyume.Action.Guild.Migration.MigrateDelegation(target);
        try
        {
            action.Execute(new ActionContext
            {
                IsPolicyAction = false,
                PreviousState = worldEmpty,
                Signer = signer,
            });
        }
        catch
        {
            // Do nothing.
        }
    }

    [Benchmark]
    public void Execute_Before_Guild_Migration()
    {
        var action = new Nekoyume.Action.Guild.Migration.MigrateDelegation(target);
        try
        {
            action.Execute(new ActionContext
            {
                IsPolicyAction = false,
                PreviousState = worldBeforeGuildMigration,
                Signer = signer,
            });
        }
        catch
        {
            // Do nothing.
        }
    }

    [Benchmark]
    public void Execute_Before_Participant_Migration()
    {
        var action = new Nekoyume.Action.Guild.Migration.MigrateDelegation(target);
        try
        {
            action.Execute(new ActionContext
            {
                IsPolicyAction = false,
                PreviousState = worldBeforeParticipantMigration,
                Signer = signer,
            });
        }
        catch
        {
            // Do nothing.
        }
    }

    [Benchmark]
    public void Execute_After_Migration()
    {
        var action = new Nekoyume.Action.Guild.Migration.MigrateDelegation(target);
        try
        {
            action.Execute(new ActionContext
            {
                IsPolicyAction = false,
                PreviousState = worldAfterMigration,
                Signer = signer,
            });
        }
        catch
        {
            // Do nothing.
        }
    }
}
