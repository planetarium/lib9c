using BenchmarkDotNet.Attributes;
using Lib9c.Tests.Action;
using Lib9c.Tests.Util;
using Lib9c.TypedAddress;
using Libplanet.Action.State;
using Libplanet.Mocks;
using Libplanet.Types.Assets;

namespace Lib9c.Benchmarks.Actions;

public class TransferAsset
{
    private AgentAddress signer = AddressUtil.CreateAgentAddress();
    private AgentAddress recipient = AddressUtil.CreateAgentAddress();
    private Currency currency = Currency.Uncapped("NCG", 2, null);
    private IWorld world;

    [GlobalSetup]
    public void Setup()
    {
        world = new World(MockUtil.MockModernWorldState)
            .MintAsset(new ActionContext(), signer, currency * 100);
    }

    [Benchmark]
    public void Execute()
    {
        var action = new Action.TransferAsset(signer, recipient, currency * 100);
        action.Execute(new ActionContext
        {
            Signer = signer,
            PreviousState = world,
            IsPolicyAction = false,
        });
    }
}
