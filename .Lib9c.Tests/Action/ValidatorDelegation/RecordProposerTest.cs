#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using Lib9c.Action.ValidatorDelegation;
using Lib9c.Module.ValidatorDelegation;
using Lib9c.ValidatorDelegation;
using Libplanet.Crypto;
using Xunit;

public class RecordProposerTest : ValidatorDelegationTestBase
{
    [Fact]
    public void Serialization()
    {
        var action = new RecordProposer();
        var plainValue = action.PlainValue;

        var deserialized = new RecordProposer();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        // Given
        var world = World;
        var minerKey = new PrivateKey();
        var blockIndex = (long)Random.Shared.Next(1, 100);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockIndex++,
            Miner = minerKey.Address,
        };
        var recordProposer = new RecordProposer();
        world = recordProposer.Execute(actionContext);

        // Then
        var repository = new ValidatorRepository(world, actionContext);
        var proposerInfo = repository.GetProposerInfo();

        Assert.Equal(blockIndex - 1, proposerInfo.BlockIndex);
        Assert.Equal(minerKey.Address, proposerInfo.Proposer);
    }
}
