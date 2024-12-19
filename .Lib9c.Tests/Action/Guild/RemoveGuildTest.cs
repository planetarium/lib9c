namespace Lib9c.Tests.Action.Guild;

using System;
using Lib9c.Tests.Util;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Action.Guild;
using Nekoyume.Model.Guild;
using Nekoyume.Module.Guild;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class RemoveGuildTest : GuildTestBase
{
    [Fact]
    public void Serialization()
    {
        var action = new RemoveGuild();
        var plainValue = action.PlainValue;

        var deserialized = new RemoveGuild();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);

        // When
        var removeGuild = new RemoveGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };
        world = removeGuild.Execute(actionContext);

        // Then
        var guildRepository = new GuildRepository(world, actionContext);
        var validatorRepository = new ValidatorRepository(world, actionContext);
        var guildDelegatee = guildRepository.GetDelegatee(validatorKey.Address);
        var validatorDelegatee = validatorRepository.GetDelegatee(validatorKey.Address);

        Assert.Throws<FailedLoadStateException>(() => guildRepository.GetGuild(guildAddress));
        Assert.Equal(0, guildDelegatee.TotalShares);
        Assert.Equal(0, validatorDelegatee.TotalShares);
    }

    [Fact]
    public void Execute_UnknownGuild_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToSetGuildParticipant(world, guildMasterAddress, guildAddress);

        // When
        var removeGuild = new RemoveGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => removeGuild.Execute(actionContext));
        Assert.Equal("There is no such guild.", exception.Message);
    }

    [Fact]
    public void Execute_ByGuildMember_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToJoinGuild(world, guildAddress, guildMemberAddress, 1L);

        // When
        var removeGuild = new RemoveGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMemberAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => removeGuild.Execute(actionContext));
        Assert.Equal("The signer is not a guild master.", exception.Message);
    }

    [Fact]
    public void Execute_GuildHasMember_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildParticipantAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToPrepareGuildGold(world, guildMasterAddress, GG * 100);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToJoinGuild(world, guildAddress, guildParticipantAddress, 1L);

        // When
        var removeGuild = new RemoveGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => removeGuild.Execute(actionContext));
        Assert.Equal("There are remained participants in the guild.", exception.Message);
    }

    [Fact]
    public void Execute_GuildHasBond_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToPrepareGuildGold(world, guildMasterAddress, GG * 100);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);

        // When
        var removeGuild = new RemoveGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => removeGuild.Execute(actionContext));
        Assert.Equal("The signer has a bond with the validator.", exception.Message);
    }

    [Fact]
    public void Execute_ByNonGuildMember_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var otherAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);

        // When
        var removeGuild = new RemoveGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = otherAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => removeGuild.Execute(actionContext));
        Assert.Equal("The signer does not join any guild.", exception.Message);
    }

    [Fact]
    public void Execute_ResetBannedAddresses()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var bannedAddress = AddressUtil.CreateAgentAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToJoinGuild(world, guildAddress, bannedAddress, 1L);
        world = EnsureToBanGuildMember(world, guildMasterAddress, bannedAddress);

        // When
        var removeGuild = new RemoveGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };
        world = removeGuild.Execute(actionContext);

        // Then
        var repository = new GuildRepository(world, actionContext);
        Assert.False(repository.IsBanned(guildAddress, bannedAddress));
    }
}
