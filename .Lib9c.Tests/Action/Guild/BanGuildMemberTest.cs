namespace Lib9c.Tests.Action.Guild;

using System;
using Lib9c.Tests.Util;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Guild;
using Nekoyume.Model.Guild;
using Nekoyume.Module.Guild;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class BanGuildMemberTest : GuildTestBase
{
    [Fact]
    public void Serialization()
    {
        var guildMemberAddress = AddressUtil.CreateAgentAddress();
        var action = new BanGuildMember(guildMemberAddress);
        var plainValue = action.PlainValue;

        var deserialized = new BanGuildMember();
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
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToPrepareGuildGold(world, targetGuildMemberAddress, GG * 100);
        world = EnsureToJoinGuild(world, guildAddress, targetGuildMemberAddress, 1L);

        // When
        var banGuildMember = new BanGuildMember(targetGuildMemberAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
            BlockIndex = 2L,
        };
        world = banGuildMember.Execute(actionContext);

        // Then
        var guildRepository = new GuildRepository(world, actionContext);
        var validatorRepository = new ValidatorRepository(world, actionContext);
        var guildDelegatee = guildRepository.GetDelegatee(validatorKey.Address);
        var validatorDelegatee = validatorRepository.GetDelegatee(validatorKey.Address);

        Assert.True(guildRepository.IsBanned(guildAddress, targetGuildMemberAddress));
        Assert.Null(guildRepository.GetJoinedGuild(targetGuildMemberAddress));
        Assert.Equal(0, guildDelegatee.TotalShares);
        Assert.Equal(0, validatorDelegatee.TotalShares);
    }

    [Fact]
    public void Execute_NotMember()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);

        // When
        var banGuildMember = new BanGuildMember(guildMemberAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
            BlockIndex = 2L,
        };
        world = banGuildMember.Execute(actionContext);

        // Then
        var guildRepository = new GuildRepository(world, actionContext);
        Assert.True(guildRepository.IsBanned(guildAddress, guildMemberAddress));
        Assert.Null(guildRepository.GetJoinedGuild(guildMemberAddress));
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
        var banGuildMember = new BanGuildMember(guildMemberAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => banGuildMember.Execute(actionContext));
        Assert.Equal("The signer does not have a guild.", exception.Message);
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
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToSetGuildParticipant(world, guildMasterAddress, unknownGuildAddress);

        // When
        var banGuildMember = new BanGuildMember(guildMemberAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => banGuildMember.Execute(actionContext));
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
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToJoinGuild(world, guildAddress, guildMemberAddress, 1L);

        // When
        var banGuildMember = new BanGuildMember(guildMemberAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMemberAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => banGuildMember.Execute(actionContext));
        Assert.Equal("The signer is not a guild master.", exception.Message);
    }

    [Fact]
    public void Execute_TargetIsGuildMaster_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);

        // When
        var banGuildMember = new BanGuildMember(guildMasterAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => banGuildMember.Execute(actionContext));
        Assert.Equal("The guild master cannot be banned.", exception.Message);
    }

    // Expected use-case.
    [Fact]
    public void Ban_By_GuildMaster()
    {
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var otherGuildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildMemberAddress = AddressUtil.CreateAgentAddress();
        var otherGuildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var otherGuildAddress = AddressUtil.CreateGuildAddress();

        IWorld world = World;
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToJoinGuild(world, guildAddress, guildMemberAddress, 1L);
        world = EnsureToMakeGuild(world, otherGuildAddress, otherGuildMasterAddress, validatorKey.Address);
        world = EnsureToJoinGuild(world, otherGuildAddress, otherGuildMemberAddress, 1L);

        var repository = new GuildRepository(world, new ActionContext());
        // Guild
        Assert.False(repository.IsBanned(guildAddress, guildMasterAddress));
        Assert.Equal(guildAddress, repository.GetJoinedGuild(guildMasterAddress));
        Assert.False(repository.IsBanned(guildAddress, guildMemberAddress));
        Assert.Equal(guildAddress, repository.GetJoinedGuild(guildMemberAddress));
        // Other guild
        Assert.False(repository.IsBanned(guildAddress, otherGuildMasterAddress));
        Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMasterAddress));
        Assert.False(repository.IsBanned(guildAddress, otherGuildMemberAddress));
        Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMemberAddress));

        var action = new BanGuildMember(guildMemberAddress);
        world = action.Execute(new ActionContext
        {
            PreviousState = repository.World,
            Signer = guildMasterAddress,
        });

        // Guild
        repository.UpdateWorld(world);
        Assert.False(repository.IsBanned(guildAddress, guildMasterAddress));
        Assert.Equal(guildAddress, repository.GetJoinedGuild(guildMasterAddress));
        Assert.True(repository.IsBanned(guildAddress, guildMemberAddress));
        Assert.Null(repository.GetJoinedGuild(guildMemberAddress));
        // Other guild
        Assert.False(repository.IsBanned(guildAddress, otherGuildMasterAddress));
        Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMasterAddress));
        Assert.False(repository.IsBanned(guildAddress, otherGuildMemberAddress));
        Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMemberAddress));

        action = new BanGuildMember(otherGuildMasterAddress);
        world = action.Execute(new ActionContext
        {
            PreviousState = repository.World,
            Signer = guildMasterAddress,
        });

        // Guild
        repository.UpdateWorld(world);
        Assert.False(repository.IsBanned(guildAddress, guildMasterAddress));
        Assert.Equal(guildAddress, repository.GetJoinedGuild(guildMasterAddress));
        Assert.True(repository.IsBanned(guildAddress, guildMemberAddress));
        Assert.Null(repository.GetJoinedGuild(guildMemberAddress));
        // Other guild
        Assert.True(repository.IsBanned(guildAddress, otherGuildMasterAddress));
        Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMasterAddress));
        Assert.False(repository.IsBanned(guildAddress, otherGuildMemberAddress));
        Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMemberAddress));

        action = new BanGuildMember(otherGuildMemberAddress);
        world = action.Execute(new ActionContext
        {
            PreviousState = repository.World,
            Signer = guildMasterAddress,
        });

        // Guild
        repository.UpdateWorld(world);
        Assert.False(repository.IsBanned(guildAddress, guildMasterAddress));
        Assert.Equal(guildAddress, repository.GetJoinedGuild(guildMasterAddress));
        Assert.True(repository.IsBanned(guildAddress, guildMemberAddress));
        Assert.Null(repository.GetJoinedGuild(guildMemberAddress));
        // Other guild
        Assert.True(repository.IsBanned(guildAddress, otherGuildMasterAddress));
        Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMasterAddress));
        Assert.True(repository.IsBanned(guildAddress, otherGuildMemberAddress));
        Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMemberAddress));

        action = new BanGuildMember(guildMasterAddress);
        // GuildMaster cannot ban itself.
        Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
        {
            PreviousState = repository.World,
            Signer = guildMasterAddress,
        }));
    }

    [Fact]
    public void Ban_By_GuildMember()
    {
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildMemberAddress = AddressUtil.CreateAgentAddress();
        var otherAddress = AddressUtil.CreateAgentAddress();
        var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();

        var action = new BanGuildMember(targetGuildMemberAddress);

        IWorld world = World;
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToJoinGuild(world, guildAddress, guildMemberAddress, 1L);
        world = EnsureToJoinGuild(world, guildAddress, targetGuildMemberAddress, 1L);

        var repository = new GuildRepository(world, new ActionContext());

        // GuildMember tries to ban other guild member.
        Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
        {
            PreviousState = repository.World,
            Signer = guildMemberAddress,
        }));

        // GuildMember tries to ban itself.
        action = new BanGuildMember(guildMemberAddress);
        Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
        {
            PreviousState = repository.World,
            Signer = guildMemberAddress,
        }));

        action = new BanGuildMember(otherAddress);
        // GuildMember tries to ban other not joined to its guild.
        Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
        {
            PreviousState = repository.World,
            Signer = guildMemberAddress,
        }));
    }

    [Fact]
    public void Ban_By_Other()
    {
        // NOTE: It assumes 'other' hasn't any guild. If 'other' has its own guild,
        //       it should be assumed as a guild master.
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var otherAddress = AddressUtil.CreateAgentAddress();
        var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();

        IWorld world = World;
        world = EnsureToCreateValidator(world, validatorKey.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToJoinGuild(world, guildAddress, targetGuildMemberAddress, 1L);

        var repository = new GuildRepository(world, new ActionContext());

        // Other tries to ban GuildMember.
        var action = new BanGuildMember(targetGuildMemberAddress);
        Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
        {
            PreviousState = repository.World,
            Signer = otherAddress,
        }));

        // Other tries to ban GuildMaster.
        action = new BanGuildMember(guildMasterAddress);
        Assert.Throws<InvalidOperationException>(
            () => action.Execute(
                new ActionContext
                {
                    PreviousState = world,
                    Signer = otherAddress,
                }));
    }
}
