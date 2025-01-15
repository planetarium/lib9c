namespace Lib9c.Tests.Action.Guild;

using System;
using System.Collections.Generic;
using System.Numerics;
using Lib9c.Tests.Util;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Action.Guild;
using Nekoyume.Model.Guild;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class QuitGuildTest : GuildTestBase
{
    private interface IQuitGuildFixture
    {
        public PrivateKey ValidatorKey { get; }

        public FungibleAssetValue ValidatorGG { get; }

        public BigInteger SlashFactor { get; }

        public AgentAddress AgentAddress { get; }

        public FungibleAssetValue AgentNCG { get; }

        public GuildAddress GuildAddress { get; }

        public AgentAddress GuildMasterAddress { get; }

        public FungibleAssetValue GuildMasterNCG { get; }
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
        var action = new QuitGuild();
        var plainValue = action.PlainValue;

        var deserialized = new QuitGuild();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        var fixture = new StaticFixture
        {
            ValidatorGG = GG * 100,
            SlashFactor = 0,
            AgentNCG = NCG * 100,
            GuildMasterNCG = NCG * 100,
        };
        ExecuteWithFixture(fixture);
    }

    [Fact]
    public void Execute_SlashedValidator()
    {
        var fixture = new StaticFixture
        {
            ValidatorGG = GG * 100,
            SlashFactor = 10,
            AgentNCG = NCG * 100,
            GuildMasterNCG = NCG * 100,
        };
        ExecuteWithFixture(fixture);
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
        world = EnsureToPrepareGuildGold(world, validatorKey.Address, GG * 100);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, GG * 100);
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
        world = EnsureToPrepareGuildGold(world, validatorKey.Address, GG * 100);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, GG * 100);
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

    [Theory]
    [InlineData(0)]
    [InlineData(1181126949)]
    [InlineData(793705868)]
    [InlineData(559431555)]
    [InlineData(1746916991)]
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

    private void ExecuteWithFixture(IQuitGuildFixture fixture)
    {
        // Given
        var world = World;
        var validatorKey = fixture.ValidatorKey;
        var validatorAddress = validatorKey.Address;
        var validatorGG = fixture.ValidatorGG;
        var slashFactor = fixture.SlashFactor;
        var agentAddress = fixture.AgentAddress;
        var agentNCG = fixture.AgentNCG;
        var agentAmount = agentNCG.MajorUnit;
        var agentGG = NCGToGG(agentNCG);
        var guildMasterAddress = fixture.GuildMasterAddress;
        var guildMasterNCG = fixture.GuildMasterNCG;
        var guildMasterAmount = guildMasterNCG.MajorUnit;
        var guildMasterGG = NCGToGG(guildMasterNCG);
        var guildAddress = fixture.GuildAddress;
        var height = 0L;
        var avatarIndex = 0;
        world = EnsureToPrepareGuildGold(world, validatorKey.Address, validatorGG);
        world = EnsureToMintAsset(world, guildMasterAddress, guildMasterNCG);
        world = EnsureToMintAsset(world, agentAddress, agentNCG);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, validatorGG);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToCreateAvatar(world, guildMasterAddress, avatarIndex: 0);
        world = EnsureToCreateAvatar(world, agentAddress, avatarIndex: 0);
        world = EnsureToStake(world, guildMasterAddress, avatarIndex, guildMasterAmount, height++);
        world = EnsureToStake(world, agentAddress, avatarIndex, agentAmount, height++);
        world = EnsureToJoinGuild(world, guildAddress, agentAddress, height++);
        if (slashFactor > 0)
        {
            world = EnsureToSlashValidator(world, validatorAddress, slashFactor, height++);
        }

        // When
        var totalGG = validatorGG + guildMasterGG + agentGG;
        var slashedGG = SlashFAV(slashFactor, totalGG);
        var totalShare = totalGG.RawValue;
        var agentShare = totalShare * agentGG.RawValue / totalGG.RawValue;
        var expectedAgengGG = (slashedGG * agentShare).DivRem(totalShare).Quotient;
        var expectedTotalGG = slashedGG - expectedAgengGG;
        var expectedTotalShares = totalShare - agentShare;
        var quitGuild = new QuitGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = agentAddress,
            BlockIndex = height++,
        };
        world = quitGuild.Execute(actionContext);
        world = EnsureToReleaseUnbonding(
            world, agentAddress, height + ValidatorDelegatee.ValidatorUnbondingPeriod);

        // Then
        var guildRepository = new GuildRepository(world, actionContext);
        var validatorRepository = new ValidatorRepository(world, actionContext);
        var guildDelegatee = guildRepository.GetDelegatee(validatorKey.Address);
        var validatorDelegatee = validatorRepository.GetDelegatee(validatorKey.Address);

        Assert.Throws<FailedLoadStateException>(
            () => guildRepository.GetGuildParticipant(agentAddress));
        Assert.Equal(expectedTotalGG, guildDelegatee.TotalDelegated);
        Assert.Equal(expectedTotalShares, guildDelegatee.TotalShares);
        Assert.Equal(expectedTotalGG, validatorDelegatee.TotalDelegated);
        Assert.Equal(expectedTotalShares, validatorDelegatee.TotalShares);
    }

    private class StaticFixture : IQuitGuildFixture
    {
        public PrivateKey ValidatorKey { get; set; } = new PrivateKey();

        public FungibleAssetValue ValidatorGG { get; set; } = GG * 100;

        public BigInteger SlashFactor { get; set; } = 0;

        public AgentAddress AgentAddress { get; set; } = AddressUtil.CreateAgentAddress();

        public FungibleAssetValue AgentNCG { get; set; } = NCG * 100;

        public GuildAddress GuildAddress { get; set; } = AddressUtil.CreateGuildAddress();

        public AgentAddress GuildMasterAddress { get; set; } = AddressUtil.CreateAgentAddress();

        public FungibleAssetValue GuildMasterNCG { get; set; } = NCG * 100;
    }

    private class RandomFixture : IQuitGuildFixture
    {
        private readonly Random _random;

        public RandomFixture(int randomSeed)
        {
            _random = new Random(randomSeed);
            ValidatorKey = GetRandomKey(_random);
            ValidatorGG = GetRandomGG(_random);
            SlashFactor = GetRandomSlashFactor(_random);
            AgentAddress = GetRandomAgentAddress(_random);
            AgentNCG = GetRandomNCG(_random);
            GuildAddress = GetRandomGuildAddress(_random);
            GuildMasterAddress = GetRandomAgentAddress(_random);
            GuildMasterNCG = GetRandomNCG(_random);
        }

        public PrivateKey ValidatorKey { get; }

        public FungibleAssetValue ValidatorGG { get; }

        public BigInteger SlashFactor { get; }

        public AgentAddress AgentAddress { get; }

        public FungibleAssetValue AgentNCG { get; }

        public GuildAddress GuildAddress { get; }

        public AgentAddress GuildMasterAddress { get; }

        public FungibleAssetValue GuildMasterNCG { get; }
    }
}
