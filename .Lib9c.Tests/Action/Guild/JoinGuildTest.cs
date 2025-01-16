namespace Lib9c.Tests.Action.Guild;

using System;
using System.Collections.Generic;
using System.Numerics;
using Lib9c.Tests.Util;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Guild;
using Nekoyume.Model.Guild;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class JoinGuildTest : GuildTestBase
{
    private interface IJoinGuildFixture
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
    public void Execute_SlashedValidator()
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
    public void Execute_SignerIsAlreadyJoinedGuild_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var agentAddress = AddressUtil.CreateAgentAddress();
        var masterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var height = 1L;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, masterAddress, validatorKey, height++);
        world = EnsureToInitializeAgent(world, agentAddress, NCG * 100, height++);
        world = EnsureToJoinGuild(world, guildAddress, agentAddress, height++);

        // When
        var joinGuild = new JoinGuild(guildAddress);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = agentAddress,
            BlockIndex = height,
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
        var masterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var height = 1L;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, masterAddress, validatorKey, height++);
        world = EnsureToInitializeAgent(world, agentAddress, NCG * 100, height++);
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
        var masterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var height = 0L;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);
        world = EnsureToMakeGuild(world, guildAddress, masterAddress, validatorKey, height++);
        world = EnsureToInitializeAgent(world, agentAddress, NCG * 100, height++);

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

    [Theory]
    [InlineData(0)]
    [InlineData(1181126949)]
    [InlineData(793705868)]
    [InlineData(559431555)]
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

    private void ExecuteWithFixture(IJoinGuildFixture fixture)
    {
        // Given
        var world = World;
        var validatorKey = fixture.ValidatorKey;
        var validatorAddress = validatorKey.Address;
        var validatorNCG = fixture.ValidatorNCG;
        var validatorGG = NCGToGG(validatorNCG);
        var slashFactor = fixture.SlashFactor;
        var agentAddress = fixture.AgentAddress;
        var agentNCG = fixture.AgentNCG;
        var agentGG = NCGToGG(agentNCG);
        var masterAddress = fixture.MasterAddress;
        var masterNCG = fixture.MasterNCG;
        var masterGG = NCGToGG(masterNCG);
        var guildAddress = fixture.GuildAddress;
        var height = 0L;
        world = EnsureToInitializeValidator(world, validatorKey, validatorNCG, height++);
        world = EnsureToInitializeAgent(world, masterAddress, masterNCG, height++);
        world = EnsureToInitializeAgent(world, agentAddress, agentNCG, height++);
        world = EnsureToMakeGuild(world, guildAddress, masterAddress, validatorKey, height++);
        world = EnsureToStake(world, masterAddress, masterNCG, height++);
        world = EnsureToStake(world, agentAddress, agentNCG, height++);
        if (slashFactor > 1)
        {
            world = EnsureToSlashValidator(world, validatorAddress, slashFactor, height++);
        }

        // When
        var totalGG = validatorGG + masterGG;
        var slashedGG = SlashFAV(slashFactor, validatorGG + masterGG);
        var totalShare = totalGG.RawValue;
        var agentShare = totalShare * agentGG.RawValue / slashedGG.RawValue;
        var expectedTotalGG = slashedGG + agentGG;
        var expectedTotalShares = totalShare + agentShare;
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
        Assert.Equal(expectedTotalGG, guildDelegatee.TotalDelegated);
        Assert.Equal(expectedTotalShares, guildDelegatee.TotalShares);
        Assert.Equal(expectedTotalGG, validatorDelegatee.TotalDelegated);
        Assert.Equal(expectedTotalShares, validatorDelegatee.TotalShares);
    }

    private class StaticFixture : IJoinGuildFixture
    {
        public PrivateKey ValidatorKey { get; set; } = new PrivateKey();

        public FungibleAssetValue ValidatorNCG { get; set; } = NCG * 100;

        public BigInteger SlashFactor { get; set; }

        public AgentAddress AgentAddress { get; set; } = AddressUtil.CreateAgentAddress();

        public FungibleAssetValue AgentNCG { get; set; } = NCG * 100;

        public GuildAddress GuildAddress { get; set; } = AddressUtil.CreateGuildAddress();

        public AgentAddress MasterAddress { get; set; } = AddressUtil.CreateAgentAddress();

        public FungibleAssetValue MasterNCG { get; set; } = NCG * 100;
    }

    private class RandomFixture : IJoinGuildFixture
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
