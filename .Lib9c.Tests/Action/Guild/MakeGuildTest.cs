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

        public FungibleAssetValue ValidatorNCG { get; }

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
            ValidatorNCG = NCG * 100,
            SlashFactor = 0,
            MasterAddress = AddressUtil.CreateAgentAddress(),
            MasterNCG = NCG * 100,
        };
        ExecuteWithFixture(fixture);
    }

    [Fact]
    public void Execute_SlashedValidator()
    {
        var fixture = new StaticFixture
        {
            ValidatorKey = new PrivateKey(),
            ValidatorNCG = NCG * 100,
            SlashFactor = 10,
            MasterAddress = AddressUtil.CreateAgentAddress(),
            MasterNCG = NCG * 100,
        };
        ExecuteWithFixture(fixture);
    }

    [Fact]
    public void Execute_AlreadyGuildOwner_Throw()
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
        var makeGuild = new MakeGuild(validatorKey.Address);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = masterAddress,
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
        var masterAddress = AddressUtil.CreateAgentAddress();
        var height = 0L;
        world = EnsureToInitializeValidator(world, validatorKey, NCG * 100, height++);

        // When
        var makeGuild = new MakeGuild(validatorKey.Address);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
            BlockIndex = height,
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
        var masterAddress = AddressUtil.CreateAgentAddress();

        // When
        var makeGuild = new MakeGuild(validatorPrivateKey.Address);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = masterAddress,
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
        var masterAddress = fixture.MasterAddress;
        var validatorNCG = fixture.ValidatorNCG;
        var validatorGG = NCGToGG(validatorNCG);
        var masterNCG = fixture.MasterNCG;
        var masterGG = NCGToGG(masterNCG);
        var slashFactor = fixture.SlashFactor;
        var height = 0L;
        world = EnsureToInitializeValidator(world, validatorKey, validatorNCG, height++);
        world = EnsureToInitializeAgent(world, masterAddress, masterNCG, height++);
        world = EnsureToStake(world, masterAddress, masterNCG, height++);
        if (slashFactor > 0)
        {
            world = EnsureToSlashValidator(world, validatorKey, slashFactor, height++);
        }

        // When
        var totalGG = validatorGG;
        var slashedGG = SlashFAV(slashFactor, validatorGG);
        var totalShare = totalGG.RawValue;
        var agentShare = totalShare * masterGG.RawValue / slashedGG.RawValue;
        var expectedTotalGG = slashedGG + masterGG;
        var expectedTotalShares = totalShare + agentShare;
        var makeGuild = new MakeGuild(validatorKey.Address);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = masterAddress,
            BlockIndex = height,
        };
        world = makeGuild.Execute(actionContext);

        // Then
        var guildRepository = new GuildRepository(world, new ActionContext());
        var validatorRepository = new ValidatorRepository(world, new ActionContext());
        var guildDelegatee = guildRepository.GetDelegatee(validatorKey.Address);
        var validatorDelegatee = validatorRepository.GetDelegatee(validatorKey.Address);
        var guildAddress = guildRepository.GetJoinedGuild(masterAddress);
        Assert.NotNull(guildAddress);
        var guild = guildRepository.GetGuild(guildAddress.Value);
        Assert.Equal(masterAddress, guild.GuildMasterAddress);
        Assert.Equal(expectedTotalGG, guildDelegatee.TotalDelegated);
        Assert.Equal(expectedTotalShares, guildDelegatee.TotalShares);
        Assert.Equal(expectedTotalGG, validatorDelegatee.TotalDelegated);
        Assert.Equal(expectedTotalShares, validatorDelegatee.TotalShares);
    }

    private class StaticFixture : IMakeGuildFixture
    {
        public PrivateKey ValidatorKey { get; set; } = new PrivateKey();

        public FungibleAssetValue ValidatorNCG { get; set; } = NCG * 100;

        public BigInteger SlashFactor { get; set; }

        public AgentAddress MasterAddress { get; set; } = AddressUtil.CreateAgentAddress();

        public FungibleAssetValue MasterNCG { get; set; } = GG * 100;
    }

    private class RandomFixture : IMakeGuildFixture
    {
        private readonly Random _random;

        public RandomFixture(int randomSeed)
        {
            _random = new Random(randomSeed);
            ValidatorKey = GetRandomKey(_random);
            ValidatorNCG = GetRandomNCG(_random);
            SlashFactor = GetRandomSlashFactor(_random);
            MasterAddress = GetRandomAgentAddress(_random);
            MasterNCG = GetRandomNCG(_random);
        }

        public PrivateKey ValidatorKey { get; }

        public FungibleAssetValue ValidatorNCG { get; }

        public BigInteger SlashFactor { get; }

        public AgentAddress MasterAddress { get; }

        public FungibleAssetValue MasterNCG { get; }
    }
}
