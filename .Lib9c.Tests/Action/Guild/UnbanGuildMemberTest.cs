namespace Lib9c.Tests.Action.Guild;

using System;
using Lib9c.Tests.Util;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Lib9c.Action.Guild;
using Lib9c.Model.Guild;
using Lib9c.Module.Guild;
using Xunit;

public class UnbanGuildMemberTest : GuildTestBase
{
    [Fact]
    public void Serialization()
    {
        var memberAddress = new PrivateKey().Address;
        var action = new UnbanGuildMember(memberAddress);
        var plainValue = action.PlainValue;

        var deserialized = new UnbanGuildMember();
        deserialized.LoadPlainValue(plainValue);
        Assert.Equal(memberAddress, deserialized.Target);
    }

    [Fact]
    public void Execute()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var masterAddress = AddressUtil.CreateAgentAddress();
        var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var height = 0L;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, masterAddress, validatorKey, height++);
        world = EnsureToJoinGuild(world, guildAddress, targetGuildMemberAddress, height++);
        world = EnsureToBanMember(world, masterAddress, targetGuildMemberAddress, height++);

        // When
        var unbanGuildMember = new UnbanGuildMember(targetGuildMemberAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = masterAddress,
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
        var masterAddress = AddressUtil.CreateAgentAddress();
        var memberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();

        // When
        var unbanGuildMember = new UnbanGuildMember(memberAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = masterAddress,
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
        var masterAddress = AddressUtil.CreateAgentAddress();
        var memberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var unknownGuildAddress = AddressUtil.CreateGuildAddress();
        var height = 0L;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, masterAddress, validatorKey, height++);
        world = EnsureToSetGuildParticipant(world, masterAddress, unknownGuildAddress);

        // When
        var unbanGuildMember = new UnbanGuildMember(memberAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = masterAddress,
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
        var masterAddress = AddressUtil.CreateAgentAddress();
        var memberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var height = 0L;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, masterAddress, validatorKey, height++);
        world = EnsureToJoinGuild(world, guildAddress, memberAddress, height++);

        // When
        var unbanGuildMember = new UnbanGuildMember(memberAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = memberAddress,
            BlockIndex = height,
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
        var masterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var height = 0L;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, masterAddress, validatorKey, height++);

        // When
        var unbanGuildMember = new UnbanGuildMember(masterAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = masterAddress,
            BlockIndex = height,
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
        var masterAddress = AddressUtil.CreateAgentAddress();
        var memberAddress = AddressUtil.CreateAgentAddress();
        var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var height = 0L;

        var action = new UnbanGuildMember(targetGuildMemberAddress);

        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, masterAddress, validatorKey, height++);
        world = EnsureToJoinGuild(world, guildAddress, memberAddress, height++);
        world = EnsureToBanMember(world, masterAddress, targetGuildMemberAddress, height++);

        var repository = new GuildRepository(world, new ActionContext());

        // GuildMember tries to ban other guild member.
        Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
        {
            PreviousState = repository.World,
            Signer = memberAddress,
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
        var masterAddress = AddressUtil.CreateAgentAddress();
        var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var height = 0L;

        var action = new UnbanGuildMember(targetGuildMemberAddress);

        IWorld world = World;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, masterAddress, validatorKey, height++);
        world = EnsureToJoinGuild(world, guildAddress, targetGuildMemberAddress, height++);
        world = EnsureToBanMember(world, masterAddress, targetGuildMemberAddress, height++);

        var repository = new GuildRepository(world, new ActionContext());

        Assert.True(repository.IsBanned(guildAddress, targetGuildMemberAddress));
        Assert.Null(repository.GetJoinedGuild(targetGuildMemberAddress));

        world = action.Execute(new ActionContext
        {
            PreviousState = repository.World,
            Signer = masterAddress,
            BlockIndex = height,
        });

        repository.UpdateWorld(world);
        Assert.False(repository.IsBanned(guildAddress, targetGuildMemberAddress));
        Assert.Null(repository.GetJoinedGuild(targetGuildMemberAddress));
    }
}
