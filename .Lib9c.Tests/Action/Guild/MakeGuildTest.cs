namespace Lib9c.Tests.Action.Guild;

using System;
using System.Collections.Generic;
using Lib9c.Tests.Util;
using Libplanet.Crypto;
using Nekoyume.Action.Guild;
using Nekoyume.Model.Guild;
using Nekoyume.Module.Guild;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class MakeGuildTest : GuildTestBase
{
    public static IEnumerable<object[]> TestCases =>
        new[]
        {
            new object[]
            {
                AddressUtil.CreateAgentAddress(),
                // TODO: Update to false when Guild features are enabled.
                true,
            },
            new object[]
            {
                GuildConfig.PlanetariumGuildOwner,
                false,
            },
        };

    [Fact]
    public void Serialization()
    {
        var action = new MakeGuild();
        var plainValue = action.PlainValue;

        var deserialized = new MakeGuild();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        // Given
        var world = World;
        var validatorPrivateKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        world = EnsureToCreateValidator(world, validatorPrivateKey.PublicKey);
        world = EnsureToPrepareGuildGold(world, guildMasterAddress, GG * 100);

        // When
        var makeGuild = new MakeGuild(validatorPrivateKey.Address);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };
        world = makeGuild.Execute(actionContext);

        // Then
        var guildRepository = new GuildRepository(world, new ActionContext());
        var validatorRepository = new ValidatorRepository(world, new ActionContext());
        var guildDelegatee = guildRepository.GetDelegatee(validatorPrivateKey.Address);
        var validatorDelegatee = validatorRepository.GetDelegatee(validatorPrivateKey.Address);
        var guildAddress = guildRepository.GetJoinedGuild(guildMasterAddress);
        Assert.NotNull(guildAddress);
        var guild = guildRepository.GetGuild(guildAddress.Value);
        Assert.Equal(guildMasterAddress, guild.GuildMasterAddress);
        Assert.Equal(SharePerGG * 100, guildDelegatee.TotalShares);
        Assert.Equal(SharePerGG * 100, validatorDelegatee.TotalShares);
    }

    [Fact]
    public void Execute_AlreadyGuildOwner_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);

        // When
        var makeGuild = new MakeGuild(validatorKey.Address);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => makeGuild.Execute(actionContext));
        Assert.Equal("The signer already has a guild.", exception.Message);
    }

    [Fact]
    public void Execute_ByValidator_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);

        // When
        var makeGuild = new MakeGuild(validatorKey.Address);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => makeGuild.Execute(actionContext));
        Assert.Equal("Validator cannot make a guild.", exception.Message);
    }

    [Fact]
    public void Execute_WithUnknowValidator_Throw()
    {
        // Given
        var world = World;
        var validatorPrivateKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();

        // When
        var makeGuild = new MakeGuild(validatorPrivateKey.Address);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => makeGuild.Execute(actionContext));
        Assert.Equal("The validator does not exist.", exception.Message);
    }
}
