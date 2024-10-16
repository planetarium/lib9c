using BenchmarkDotNet.Attributes;
using Bencodex.Types;
using Lib9c.Tests.Action;
using Lib9c.Tests.Util;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Nekoyume;
using Nekoyume.Action.Guild;
using Nekoyume.Extensions;
using Nekoyume.Model.Guild;
using Nekoyume.Module;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;

namespace Lib9c.Benchmarks.Actions;

public class MigratePledgeToGuild
{
    private AgentAddress signer = AddressUtil.CreateAgentAddress();
    private AgentAddress target = AddressUtil.CreateAgentAddress();
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
                target.GetPledgeAddress(),
                new List(MeadConfig.PatronAddress.Bencoded, (Boolean)true, (Integer)4));

        var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
        var guildAddress = AddressUtil.CreateGuildAddress();
        var validatorAddress = new PrivateKey().Address;
        var repository = new GuildRepository(worldWithPledge, new ActionContext());
        worldWithPledgeAndGuild = repository
            .MakeGuild(guildAddress, guildMasterAddress, validatorAddress).World;
        worldAfterMigration = repository
            .JoinGuild(guildAddress, signer).World;
    }

    [Benchmark]
    public void Execute_WithoutPledge()
    {
        var action = new Nekoyume.Action.Guild.Migration.MigratePledgeToGuild(target);
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
    public void Execute_WithPledge_WithoutGuild()
    {
        var action = new Nekoyume.Action.Guild.Migration.MigratePledgeToGuild(target);
        try
        {
            action.Execute(new ActionContext
            {
                IsPolicyAction = false,
                PreviousState = worldWithPledge,
                Signer = signer,
            });
        }
        catch
        {
            // Do nothing.
        }
    }

    [Benchmark]
    public void Execute_WithPledge_WithGuild()
    {
        var action = new Nekoyume.Action.Guild.Migration.MigratePledgeToGuild(target);
        action.Execute(new ActionContext
        {
            IsPolicyAction = false,
            PreviousState = worldWithPledgeAndGuild,
            Signer = signer,
        });
    }

    [Benchmark]
    public void Execute_AfterMigration()
    {
        var action = new Nekoyume.Action.Guild.Migration.MigratePledgeToGuild(target);
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
