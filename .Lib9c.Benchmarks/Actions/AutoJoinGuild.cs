using BenchmarkDotNet.Attributes;
using Bencodex.Types;
using Lib9c.Tests.Action;
using Lib9c.Tests.Util;
using Libplanet.Action.State;
using Libplanet.Mocks;
using Nekoyume;
using Nekoyume.Action.Guild;
using Nekoyume.Extensions;
using Nekoyume.Module;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;

namespace Lib9c.Benchmarks.Actions;

public class AutoJoinGuild
{
    private AgentAddress signer = AddressUtil.CreateAgentAddress();
    private IWorld worldEmpty;
    private IWorld worldWithPledge;
    private IWorld worldWithPledgeAndGuild;
    private IWorld worldAfterMigration;

    [GlobalSetup]
    public void Setup()
    {
        worldEmpty = new World(MockUtil.MockModernWorldState);
        worldWithPledge = worldEmpty
            .SetLegacyState(
                signer.GetPledgeAddress(),
                new List(MeadConfig.PatronAddress.Bencoded, (Boolean)true, (Integer)4));

        var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
        var guildAddress = AddressUtil.CreateGuildAddress();
        worldWithPledgeAndGuild = worldWithPledge
            .MakeGuild(guildAddress, guildMasterAddress);
        worldAfterMigration = worldWithPledgeAndGuild
            .JoinGuild(guildAddress, signer);
    }

    [Benchmark]
    public void Execute_WithoutPledge()
    {
        var action = new Nekoyume.PolicyAction.Tx.Begin.AutoJoinGuild();
        action.Execute(new ActionContext
        {
            IsPolicyAction = true,
            PreviousState = worldEmpty,
            Signer = signer,
        });
    }

    [Benchmark]
    public void Execute_WithPledge_WithoutGuild()
    {
        var action = new Nekoyume.PolicyAction.Tx.Begin.AutoJoinGuild();
        action.Execute(new ActionContext
        {
            IsPolicyAction = true,
            PreviousState = worldWithPledge,
            Signer = signer,
        });
    }

    [Benchmark]
    public void Execute_WithPledge_WithGuild()
    {
        var action = new Nekoyume.PolicyAction.Tx.Begin.AutoJoinGuild();
        action.Execute(new ActionContext
        {
            IsPolicyAction = true,
            PreviousState = worldWithPledgeAndGuild,
            Signer = signer,
        });
    }

    [Benchmark]
    public void Execute_AfterMigration()
    {
        var action = new Nekoyume.PolicyAction.Tx.Begin.AutoJoinGuild();
        action.Execute(new ActionContext
        {
            IsPolicyAction = true,
            PreviousState = worldAfterMigration,
            Signer = signer,
        });
    }
}
