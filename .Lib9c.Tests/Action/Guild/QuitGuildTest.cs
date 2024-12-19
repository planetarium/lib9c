namespace Lib9c.Tests.Action.Guild;

using System;
using Lib9c.Tests.Util;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Action.Guild;
using Nekoyume.Model.Guild;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class QuitGuildTest : GuildTestBase
{
    [Fact]
    public void Serialization()
    {
        var action = new QuitGuild();
        var plainValue = action.PlainValue;

        var deserialized = new QuitGuild();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var agentAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToPrepareGuildGold(world, agentAddress, GG * 100);
        world = EnsureToJoinGuild(world, guildAddress, agentAddress, 1L);

        // When
        var quitGuild = new QuitGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = agentAddress,
            BlockIndex = 2L,
        };
        world = quitGuild.Execute(actionContext);

        // Then
        var guildRepository = new GuildRepository(world, actionContext);
        var validatorRepository = new ValidatorRepository(world, actionContext);
        var guildDelegatee = guildRepository.GetDelegatee(validatorKey.Address);
        var validatorDelegatee = validatorRepository.GetDelegatee(validatorKey.Address);
        Assert.Throws<FailedLoadStateException>(
            () => guildRepository.GetGuildParticipant(agentAddress));
        Assert.Equal(0, guildDelegatee.TotalShares);
        Assert.Equal(0, validatorDelegatee.TotalShares);
    }

    [Fact]
    public void Execute_SignerDoesNotHaveGuild_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var agentAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);

        // When
        var quitGuild = new QuitGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = agentAddress,
            BlockIndex = 2L,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => quitGuild.Execute(actionContext));
        Assert.Equal("The signer does not join any guild.", exception.Message);
    }

    [Fact]
    public void Execute_FromUnknownGuild_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var agentAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        world = EnsureToSetGuildParticipant(world, agentAddress, guildAddress);

        // When
        var quitGuild = new QuitGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = agentAddress,
            BlockIndex = 2L,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => quitGuild.Execute(actionContext));
        Assert.Equal("There is no such guild.", exception.Message);
    }

    [Fact]
    public void Execute_SignerIsGuildMaster_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var agentAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToPrepareGuildGold(world, guildMasterAddress, GG * 100);
        world = EnsureToJoinGuild(world, guildAddress, agentAddress, 1L);

        // When
        var quitGuild = new QuitGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
            BlockIndex = 2L,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => quitGuild.Execute(actionContext));
        Assert.Equal(
            expected: "The signer is a guild master. Guild master cannot quit the guild.",
            actual: exception.Message);
    }
}
