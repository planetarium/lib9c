namespace Lib9c.Tests.Action.Guild;

using System;
using System.Collections.Generic;
using System.Numerics;
using Lib9c.Tests.Util;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Guild;
using Nekoyume.Model.Guild;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class MakeGuildTest : GuildTestBase
{
    private interface IMakeGuildFixture
    {
        public PrivateKey ValidatorKey { get; }

        public BigInteger SlashFactor { get; }

        public FungibleAssetValue ValidatorGG { get; }

        public AgentAddress GuildMasterAddress { get; }

        public FungibleAssetValue GuildMasterGG { get; }
    }

    public static IEnumerable<object[]> RandomSeeds => new List<object[]>
    {
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
    };

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
        var fixture = new StaticFixture
        {
            ValidatorKey = new PrivateKey(),
            ValidatorGG = GG * 100,
            SlashFactor = 0,
            GuildMasterAddress = AddressUtil.CreateAgentAddress(),
            GuildMasterGG = GG * 100,
        };
        ExecuteWithFixture(fixture);
    }

    [Fact]
    public void Execute_SlashedValidator()
    {
        var fixture = new StaticFixture
        {
            ValidatorKey = new PrivateKey(),
            ValidatorGG = GG * 100,
            SlashFactor = 10,
            GuildMasterAddress = AddressUtil.CreateAgentAddress(),
            GuildMasterGG = GG * 100,
        };
        ExecuteWithFixture(fixture);
    }

    [Fact]
    public void Execute_AlreadyGuildOwner_Throw()
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
        world = EnsureToPrepareGuildGold(world, validatorKey.Address, GG * 100);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, GG * 100);

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

    private void ExecuteWithFixture(IMakeGuildFixture fixture)
    {
        // Given
        var world = World;
        var validatorKey = fixture.ValidatorKey;
        var guildMasterAddress = fixture.GuildMasterAddress;
        var validatorGG = fixture.ValidatorGG;
        var guildMasterGG = fixture.GuildMasterGG;
        var slashFactor = fixture.SlashFactor;
        var height = 0L;
        world = EnsureToPrepareGuildGold(world, validatorKey.Address, validatorGG);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, validatorGG);
        world = EnsureToPrepareGuildGold(world, guildMasterAddress, guildMasterGG);
        if (slashFactor > 0)
        {
            world = EnsureToSlashValidator(world, validatorKey.Address, slashFactor, height);
        }

        // When
        var totalGG = validatorGG;
        var slashedGG = SlashFAV(slashFactor, validatorGG);
        var totalShare = totalGG.RawValue;
        var agentShare = totalShare * guildMasterGG.RawValue / slashedGG.RawValue;
        var expectedTotalGG = slashedGG + guildMasterGG;
        var expectedTotalShares = totalShare + agentShare;
        var makeGuild = new MakeGuild(validatorKey.Address);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };
        world = makeGuild.Execute(actionContext);

        // Then
        var guildRepository = new GuildRepository(world, new ActionContext());
        var validatorRepository = new ValidatorRepository(world, new ActionContext());
        var guildDelegatee = guildRepository.GetDelegatee(validatorKey.Address);
        var validatorDelegatee = validatorRepository.GetDelegatee(validatorKey.Address);
        var guildAddress = guildRepository.GetJoinedGuild(guildMasterAddress);
        Assert.NotNull(guildAddress);
        var guild = guildRepository.GetGuild(guildAddress.Value);
        Assert.Equal(guildMasterAddress, guild.GuildMasterAddress);
        Assert.Equal(expectedTotalGG, guildDelegatee.TotalDelegated);
        Assert.Equal(expectedTotalShares, guildDelegatee.TotalShares);
        Assert.Equal(expectedTotalGG, validatorDelegatee.TotalDelegated);
        Assert.Equal(expectedTotalShares, validatorDelegatee.TotalShares);
    }

    private class StaticFixture : IMakeGuildFixture
    {
        public PrivateKey ValidatorKey { get; set; } = new PrivateKey();

        public FungibleAssetValue ValidatorGG { get; set; } = GG * 100;

        public BigInteger SlashFactor { get; set; }

        public AgentAddress GuildMasterAddress { get; set; } = AddressUtil.CreateAgentAddress();

        public FungibleAssetValue GuildMasterGG { get; set; } = GG * 100;
    }

    private class RandomFixture : IMakeGuildFixture
    {
        private readonly Random _random;

        public RandomFixture(int randomSeed)
        {
            _random = new Random(randomSeed);
            ValidatorKey = GetRandomKey(_random);
            ValidatorGG = GetRandomGG(_random);
            SlashFactor = GetRandomSlashFactor(_random);
            GuildMasterAddress = GetRandomAgentAddress(_random);
            GuildMasterGG = GetRandomGG(_random);
        }

        public PrivateKey ValidatorKey { get; }

        public FungibleAssetValue ValidatorGG { get; }

        public BigInteger SlashFactor { get; }

        public AgentAddress GuildMasterAddress { get; }

        public FungibleAssetValue GuildMasterGG { get; }
    }
}
