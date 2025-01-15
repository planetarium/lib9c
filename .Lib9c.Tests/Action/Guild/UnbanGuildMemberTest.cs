namespace Lib9c.Tests.Action.Guild;

using System;
using Lib9c.Tests.Util;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Guild;
using Nekoyume.Model.Guild;
using Nekoyume.Module.Guild;
using Xunit;

public class UnbanGuildMemberTest : GuildTestBase
{
    [Fact]
    public void Serialization()
    {
        var guildMemberAddress = new PrivateKey().Address;
        var action = new UnbanGuildMember(guildMemberAddress);
        var plainValue = action.PlainValue;

        var deserialized = new UnbanGuildMember();
        deserialized.LoadPlainValue(plainValue);
        Assert.Equal(guildMemberAddress, deserialized.Target);
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
        world = EnsureToPrepareGuildGold(world, validatorKey.Address, GG * 100);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, GG * 100);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToJoinGuild(world, guildAddress, targetGuildMemberAddress, 1L);
        world = EnsureToBanGuildMember(world, guildMasterAddress, targetGuildMemberAddress);

        // When
        var unbanGuildMember = new UnbanGuildMember(targetGuildMemberAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };
        world = unbanGuildMember.Execute(actionContext);

        // Then
        var repository = new GuildRepository(world, actionContext);
        Assert.False(repository.IsBanned(guildAddress, targetGuildMemberAddress));
        Assert.Null(repository.GetJoinedGuild(targetGuildMemberAddress));
    }

    [Fact]
    public void Execute_SignerDoesNotHaveGuild_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();

        // When
        var unbanGuildMember = new UnbanGuildMember(guildMemberAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => unbanGuildMember.Execute(actionContext));
        Assert.Equal("The signer does not join any guild.", exception.Message);
    }

    [Fact]
    public void Execute_UnknownGuild_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var unknownGuildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToPrepareGuildGold(world, validatorKey.Address, GG * 100);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, GG * 100);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToSetGuildParticipant(world, guildMasterAddress, unknownGuildAddress);

        // When
        var unbanGuildMember = new UnbanGuildMember(guildMemberAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => unbanGuildMember.Execute(actionContext));
        Assert.Equal("There is no such guild.", exception.Message);
    }

    [Fact]
    public void Execute_SignerIsNotGuildMaster_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToPrepareGuildGold(world, validatorKey.Address, GG * 100);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, GG * 100);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToJoinGuild(world, guildAddress, guildMemberAddress, 1L);

        // When
        var unbanGuildMember = new UnbanGuildMember(guildMemberAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMemberAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => unbanGuildMember.Execute(actionContext));
        Assert.Equal("The signer is not a guild master.", exception.Message);
    }

    [Fact]
    public void Execute_TargetIsNotBanned_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToPrepareGuildGold(world, validatorKey.Address, GG * 100);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, GG * 100);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);

        // When
        var unbanGuildMember = new UnbanGuildMember(guildMasterAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => unbanGuildMember.Execute(actionContext));
        Assert.Equal("The target is not banned.", exception.Message);
    }

    [Fact]
    public void Unban_By_GuildMember()
    {
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildMemberAddress = AddressUtil.CreateAgentAddress();
        var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();

        var action = new UnbanGuildMember(targetGuildMemberAddress);

        world = EnsureToPrepareGuildGold(world, validatorKey.Address, GG * 100);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, GG * 100);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToJoinGuild(world, guildAddress, guildMemberAddress, 1L);
        world = EnsureToBanGuildMember(world, guildMasterAddress, targetGuildMemberAddress);

        var repository = new GuildRepository(world, new ActionContext());

        // GuildMember tries to ban other guild member.
        Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
        {
            PreviousState = repository.World,
            Signer = guildMemberAddress,
        }));

        // GuildMember tries to ban itself.
        Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
        {
            PreviousState = repository.World,
            Signer = targetGuildMemberAddress,
        }));
    }

    [Fact]
    public void Unban_By_GuildMaster()
    {
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();

        var action = new UnbanGuildMember(targetGuildMemberAddress);

        IWorld world = World;
        world = EnsureToPrepareGuildGold(world, validatorKey.Address, GG * 100);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, GG * 100);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToJoinGuild(world, guildAddress, targetGuildMemberAddress, 1L);
        world = EnsureToBanGuildMember(world, guildMasterAddress, targetGuildMemberAddress);

        var repository = new GuildRepository(world, new ActionContext());

        Assert.True(repository.IsBanned(guildAddress, targetGuildMemberAddress));
        Assert.Null(repository.GetJoinedGuild(targetGuildMemberAddress));

        world = action.Execute(new ActionContext
        {
            PreviousState = repository.World,
            Signer = guildMasterAddress,
        });

        repository.UpdateWorld(world);
        Assert.False(repository.IsBanned(guildAddress, targetGuildMemberAddress));
        Assert.Null(repository.GetJoinedGuild(targetGuildMemberAddress));
    }
}
