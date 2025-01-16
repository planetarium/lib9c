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
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class ClaimUnbondedTest : GuildTestBase
{
    private interface IClaimUnbondedFixture
    {
        public PrivateKey ValidatorKey { get; }

        public FungibleAssetValue ValidatorNCG { get; }

        public BigInteger SlashFactor { get; }

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
        var action = new ClaimUnbonded();
        var plainValue = action.PlainValue;

        var deserialized = new ClaimUnbonded();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        var fixture = new StaticFixture
        {
            ValidatorNCG = NCG * 100,
            SlashFactor = 0,
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
            MasterNCG = NCG * 100,
        };

        ExecuteWithFixture(fixture);
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

    private void ExecuteWithFixture(IClaimUnbondedFixture fixture)
    {
        // Given
        var world = World;
        var validatorKey = fixture.ValidatorKey;
        var validatorNCG = fixture.ValidatorNCG;
        var validatorGG = NCGToGG(validatorNCG);
        var masterAddress = fixture.MasterAddress;
        var masterNCG = fixture.MasterNCG;
        var guildAddress = fixture.GuildAddress;
        var height = 0L;
        var slashFactor = fixture.SlashFactor;
        world = EnsureToInitializeValidator(world, validatorKey, validatorNCG, height++);
        if (slashFactor > 1)
        {
            world = EnsureToSlashValidator(world, validatorKey, slashFactor, height++);
        }

        world = EnsureToInitializeAgent(world, masterAddress, masterNCG, height++);
        world = EnsureToMakeGuild(world, guildAddress, masterAddress, validatorKey, height++);
        world = EnsureToStake(world, masterAddress, masterNCG, height++);
        world = EnsureToStake(world, masterAddress, NCG * 0, height++);

        // When
        var totalGG = validatorGG;
        var slashedGG = SlashFAV(slashFactor, totalGG);
        var expectedTotalGG = slashedGG;
        var expectedTotalShares = totalGG.RawValue;
        var expectedMasterNCG = masterNCG;
        var actualMasterNCGBefore = world.GetBalance(masterAddress, NCG);
        var claimUnbonded = new ClaimUnbonded();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = masterAddress,
            BlockIndex = height + ValidatorDelegatee.ValidatorUnbondingPeriod,
        };
        world = claimUnbonded.Execute(actionContext);

        // Then
        var guildRepository = new GuildRepository(world, actionContext);
        var validatorRepository = new ValidatorRepository(world, actionContext);
        var guildDelegatee = guildRepository.GetDelegatee(validatorKey.Address);
        var validatorDelegatee = validatorRepository.GetDelegatee(validatorKey.Address);
        var actualMasterNCG = world.GetBalance(masterAddress, NCG);
        var comparerGG = new FungibleAssetValueEqualityComparer(GGEpsilon);
        var comparerNCG = new FungibleAssetValueEqualityComparer(-NCGEpsilon);

        Assert.Equal(NCG * 0, actualMasterNCGBefore);
        Assert.Equal(expectedMasterNCG, actualMasterNCG, comparerNCG);
        Assert.Equal(expectedTotalGG, guildDelegatee.TotalDelegated, comparerGG);
        Assert.Equal(expectedTotalGG, validatorDelegatee.TotalDelegated, comparerGG);
        Assert.Equal(expectedTotalShares, guildDelegatee.TotalShares);
        Assert.Equal(expectedTotalShares, validatorDelegatee.TotalShares);
    }

    private class StaticFixture : IClaimUnbondedFixture
    {
        public PrivateKey ValidatorKey { get; set; } = new PrivateKey();

        public FungibleAssetValue ValidatorNCG { get; set; } = NCG * 100;

        public BigInteger SlashFactor { get; set; }

        public GuildAddress GuildAddress { get; set; } = AddressUtil.CreateGuildAddress();

        public AgentAddress MasterAddress { get; set; } = AddressUtil.CreateAgentAddress();

        public FungibleAssetValue MasterNCG { get; set; } = NCG * 100;
    }

    private class RandomFixture : IClaimUnbondedFixture
    {
        private readonly Random _random;

        public RandomFixture(int randomSeed)
        {
            _random = new Random(randomSeed);
            ValidatorKey = GetRandomKey(_random);
            ValidatorNCG = GetRandomNCG(_random);
            SlashFactor = GetRandomSlashFactor(_random);
            GuildAddress = GetRandomGuildAddress(_random);
            MasterAddress = GetRandomAgentAddress(_random);
            MasterNCG = GetRandomNCG(_random);
        }

        public PrivateKey ValidatorKey { get; }

        public FungibleAssetValue ValidatorNCG { get; }

        public BigInteger SlashFactor { get; }

        public GuildAddress GuildAddress { get; }

        public AgentAddress MasterAddress { get; }

        public FungibleAssetValue MasterNCG { get; }
    }
}
