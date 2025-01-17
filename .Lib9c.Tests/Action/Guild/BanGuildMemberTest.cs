namespace Lib9c.Tests.Action.Guild;

using System;
using System.Collections.Generic;
using System.Numerics;
using Lib9c.Tests.Util;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Guild;
using Nekoyume.Model.Guild;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class BanGuildMemberTest : GuildTestBase
{
    private interface IBanGuildMemberFixture
    {
        public PrivateKey ValidatorKey { get; }

        public FungibleAssetValue ValidatorNCG { get; }

        public BigInteger SlashFactor { get; }

        public AgentAddress AgentAddress { get; }

        public FungibleAssetValue AgentNCG { get; }

        public GuildAddress GuildAddress { get; }

        public AgentAddress MasterAddress { get; }

        public FungibleAssetValue MasterNCG { get; }
    }

    public static IEnumerable<object[]> RandomSeeds => new List<object[]>
    {
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
    };

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
        var fixture = new StaticFixture
        {
            ValidatorNCG = NCG * 100,
            SlashFactor = 0,
            AgentNCG = NCG * 100,
            MasterNCG = NCG * 100,
        };
        ExecuteWithFixture(fixture);
    }

    [Fact]
    public void Execute_SlashValidator()
    {
        var fixture = new StaticFixture
        {
            ValidatorNCG = NCG * 100,
            SlashFactor = 10,
            AgentNCG = NCG * 100,
            MasterNCG = NCG * 100,
        };
        ExecuteWithFixture(fixture);
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
        var height = 0L;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey, height++);

        // When
        var banGuildMember = new BanGuildMember(guildMemberAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
            BlockIndex = height,
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
        var height = 0L;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey, height++);
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
    public void Execute_SignerIsNotMaster_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var height = 0L;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey, height++);
        world = EnsureToJoinGuild(world, guildAddress, guildMemberAddress, height++);

        // When
        var banGuildMember = new BanGuildMember(guildMemberAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMemberAddress,
            BlockIndex = height,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => banGuildMember.Execute(actionContext));
        Assert.Equal("The signer is not a guild master.", exception.Message);
    }

    [Fact]
    public void Execute_TargetIsMaster_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var height = 0L;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey, height++);

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
    public void Ban_By_Master()
    {
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var otherMasterAddress = AddressUtil.CreateAgentAddress();
        var guildMemberAddress = AddressUtil.CreateAgentAddress();
        var otherGuildMemberAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var otherGuildAddress = AddressUtil.CreateGuildAddress();
        var height = 0L;

        IWorld world = World;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey, height++);
        world = EnsureToJoinGuild(world, guildAddress, guildMemberAddress, height++);
        world = EnsureToMakeGuild(world, otherGuildAddress, otherMasterAddress, validatorKey, height++);
        world = EnsureToJoinGuild(world, otherGuildAddress, otherGuildMemberAddress, height++);

        var repository = new GuildRepository(world, new ActionContext());
        // Guild
        Assert.False(repository.IsBanned(guildAddress, guildMasterAddress));
        Assert.Equal(guildAddress, repository.GetJoinedGuild(guildMasterAddress));
        Assert.False(repository.IsBanned(guildAddress, guildMemberAddress));
        Assert.Equal(guildAddress, repository.GetJoinedGuild(guildMemberAddress));
        // Other guild
        Assert.False(repository.IsBanned(guildAddress, otherMasterAddress));
        Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherMasterAddress));
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
        Assert.False(repository.IsBanned(guildAddress, otherMasterAddress));
        Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherMasterAddress));
        Assert.False(repository.IsBanned(guildAddress, otherGuildMemberAddress));
        Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMemberAddress));

        action = new BanGuildMember(otherMasterAddress);
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
        Assert.True(repository.IsBanned(guildAddress, otherMasterAddress));
        Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherMasterAddress));
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
        Assert.True(repository.IsBanned(guildAddress, otherMasterAddress));
        Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherMasterAddress));
        Assert.True(repository.IsBanned(guildAddress, otherGuildMemberAddress));
        Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMemberAddress));

        action = new BanGuildMember(guildMasterAddress);
        // Master cannot ban itself.
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
        var height = 0L;

        var action = new BanGuildMember(targetGuildMemberAddress);

        IWorld world = World;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey, height++);
        world = EnsureToJoinGuild(world, guildAddress, guildMemberAddress, height++);
        world = EnsureToJoinGuild(world, guildAddress, targetGuildMemberAddress, height++);

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
        var height = 0L;

        IWorld world = World;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey, height++);
        world = EnsureToJoinGuild(world, guildAddress, targetGuildMemberAddress, height++);

        var repository = new GuildRepository(world, new ActionContext());

        // Other tries to ban GuildMember.
        var action = new BanGuildMember(targetGuildMemberAddress);
        Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
        {
            PreviousState = repository.World,
            Signer = otherAddress,
        }));

        // Other tries to ban Master.
        action = new BanGuildMember(guildMasterAddress);
        Assert.Throws<InvalidOperationException>(
            () => action.Execute(
                new ActionContext
                {
                    PreviousState = world,
                    Signer = otherAddress,
                }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1181126949)]
    [InlineData(793705868)]
    [InlineData(559431555)]
    [InlineData(696266272)]
    public void Execute_Fact_WithStaticSeed(int randomSeed)
    {
        var fixture = new RandomFixture(randomSeed);
        ExecuteWithFixture(fixture);
    }

    [Theory]
    [MemberData(nameof(RandomSeeds))]
    public void Execute_Fact_WithRandomSeed(int randomSeed)
    {
        var fixture = new RandomFixture(randomSeed);
        ExecuteWithFixture(fixture);
    }

    private void ExecuteWithFixture(IBanGuildMemberFixture fixture)
    {
        // Given
        var world = World;
        var validatorKey = fixture.ValidatorKey;
        var validatorNCG = fixture.ValidatorNCG;
        var validatorGG = NCGToGG(validatorNCG);
        var masterAddress = fixture.MasterAddress;
        var masterNCG = fixture.MasterNCG;
        var masterGG = NCGToGG(masterNCG);
        var agentAddress = fixture.AgentAddress;
        var agentNCG = fixture.AgentNCG;
        var agentGG = NCGToGG(agentNCG);
        var guildAddress = fixture.GuildAddress;
        var height = 0L;
        var slashFactor = fixture.SlashFactor;
        world = EnsureToInitializeValidator(world, validatorKey, validatorNCG, height++);
        world = EnsureToMakeGuild(world, guildAddress, masterAddress, validatorKey, height++);
        world = EnsureToInitializeAgent(world, masterAddress, masterNCG, height++);
        world = EnsureToInitializeAgent(world, agentAddress, agentNCG, height++);
        world = EnsureToStake(world, masterAddress, masterNCG, height++);
        world = EnsureToStake(world, agentAddress, agentNCG, height++);
        world = EnsureToJoinGuild(world, guildAddress, agentAddress, height++);
        if (slashFactor > 1)
        {
            world = EnsureToSlashValidator(world, validatorKey, slashFactor, height++);
        }

        // When
        var totalGG = validatorGG + masterGG + agentGG;
        var slashedGG = slashFactor > 1 ? SlashFAV(slashFactor, totalGG) : totalGG;
        var totalShare = totalGG.RawValue;
        var agentShare = totalShare * agentGG.RawValue / totalGG.RawValue;
        var expectedAgengGG = (slashedGG * agentShare).DivRem(totalShare).Quotient;
        var expectedTotalGG = slashedGG - expectedAgengGG;
        var expectedTotalShares = totalShare - agentShare;
        var banGuildMember = new BanGuildMember(agentAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = masterAddress,
            BlockIndex = height,
        };
        world = banGuildMember.Execute(actionContext);

        // Then
        var guildRepository = new GuildRepository(world, actionContext);
        var validatorRepository = new ValidatorRepository(world, actionContext);
        var guildDelegatee = guildRepository.GetDelegatee(validatorKey.Address);
        var validatorDelegatee = validatorRepository.GetDelegatee(validatorKey.Address);

        Assert.True(guildRepository.IsBanned(guildAddress, agentAddress));
        Assert.Null(guildRepository.GetJoinedGuild(agentAddress));
        Assert.Equal(expectedTotalGG, guildDelegatee.TotalDelegated);
        Assert.Equal(expectedTotalShares, guildDelegatee.TotalShares);
        Assert.Equal(expectedTotalGG, validatorDelegatee.TotalDelegated);
        Assert.Equal(expectedTotalShares, validatorDelegatee.TotalShares);
    }

    private class StaticFixture : IBanGuildMemberFixture
    {
        public PrivateKey ValidatorKey { get; set; } = new PrivateKey();

        public FungibleAssetValue ValidatorNCG { get; set; } = NCG * 100;

        public BigInteger SlashFactor { get; set; } = 0;

        public AgentAddress AgentAddress { get; set; } = AddressUtil.CreateAgentAddress();

        public FungibleAssetValue AgentNCG { get; set; } = NCG * 100;

        public GuildAddress GuildAddress { get; set; } = AddressUtil.CreateGuildAddress();

        public AgentAddress MasterAddress { get; set; } = AddressUtil.CreateAgentAddress();

        public FungibleAssetValue MasterNCG { get; set; } = NCG * 100;
    }

    private class RandomFixture : IBanGuildMemberFixture
    {
        private readonly Random _random;

        public RandomFixture(int randomSeed)
        {
            _random = new Random(randomSeed);
            ValidatorKey = GetRandomKey(_random);
            ValidatorNCG = GetRandomNCG(_random);
            SlashFactor = GetRandomSlashFactor(_random);
            AgentAddress = GetRandomAgentAddress(_random);
            AgentNCG = GetRandomNCG(_random);
            GuildAddress = GetRandomGuildAddress(_random);
            MasterAddress = GetRandomAgentAddress(_random);
            MasterNCG = GetRandomNCG(_random);
        }

        public PrivateKey ValidatorKey { get; }

        public FungibleAssetValue ValidatorNCG { get; }

        public BigInteger SlashFactor { get; }

        public AgentAddress AgentAddress { get; }

        public FungibleAssetValue AgentNCG { get; }

        public GuildAddress GuildAddress { get; }

        public AgentAddress MasterAddress { get; }

        public FungibleAssetValue MasterNCG { get; }
    }
}
