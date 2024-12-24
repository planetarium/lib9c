namespace Lib9c.Tests.Action.Guild;

using System;
using Lib9c.Tests.Util;
using Libplanet.Crypto;
using Nekoyume.Action.Guild;
using Nekoyume.Model.Guild;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class JoinGuildTest : GuildTestBase
{
    [Fact]
    public void Serialization()
    {
        var guildAddress = AddressUtil.CreateGuildAddress();
        var action = new JoinGuild(guildAddress);
        var plainValue = action.PlainValue;

        var deserialized = new JoinGuild();
        deserialized.LoadPlainValue(plainValue);
        Assert.Equal(guildAddress, deserialized.GuildAddress);
    }

    [Fact]
    public void Execute()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var agentAddress = AddressUtil.CreateAgentAddress();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToPrepareGuildGold(world, agentAddress, GG * 100);

        // When
        var joinGuild = new JoinGuild(guildAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = agentAddress,
        };
        world = joinGuild.Execute(actionContext);

        // Then
        var guildRepository = new GuildRepository(world, new ActionContext());
        var guildDelegatee = guildRepository.GetDelegatee(validatorKey.Address);
        var validatorRepository = new ValidatorRepository(world, new ActionContext());
        var validatorDelegatee = validatorRepository.GetDelegatee(validatorKey.Address);
        var guildParticipant = guildRepository.GetGuildParticipant(agentAddress);

        Assert.Equal(agentAddress, guildParticipant.Address);
        Assert.Equal(guildDelegatee.TotalShares, 100 * SharePerGG);
        Assert.Equal(validatorDelegatee.TotalShares, 100 * SharePerGG);
    }

    [Fact]
    public void Execute_SignerIsAlreadyJoinedGuild_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var agentAddress = AddressUtil.CreateAgentAddress();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToPrepareGuildGold(world, agentAddress, GG * 100);
        world = EnsureToJoinGuild(world, guildAddress, agentAddress, 1L);

        // When
        var joinGuild = new JoinGuild(guildAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = agentAddress,
            BlockIndex = 2L,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => joinGuild.Execute(actionContext));
        Assert.Equal("The signer already joined a guild.", exception.Message);
    }

    [Fact]
    public void Execute_SignerHasRejoinCooldown_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var agentAddress = AddressUtil.CreateAgentAddress();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var height = 1L;
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToPrepareGuildGold(world, agentAddress, GG * 100);
        world = EnsureToJoinGuild(world, guildAddress, agentAddress, height++);
        world = EnsureToLeaveGuild(world, agentAddress, height);

        // When
        var joinGuild = new JoinGuild(guildAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = agentAddress,
            BlockIndex = height + GuildRejoinCooldown.CooldownPeriod - 1,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => joinGuild.Execute(actionContext));
        var expectedReleaseHeight = height + GuildRejoinCooldown.CooldownPeriod;
        var expectedMessage
            = $"The signer is in the rejoin cooldown period until block {expectedReleaseHeight}";
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void Execute_SignerIsValidator_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var agentAddress = AddressUtil.CreateAgentAddress();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToPrepareGuildGold(world, agentAddress, GG * 100);

        // When
        var joinGuild = new JoinGuild(guildAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => joinGuild.Execute(actionContext));
        Assert.Equal("Validator cannot join a guild.", exception.Message);
    }
}
