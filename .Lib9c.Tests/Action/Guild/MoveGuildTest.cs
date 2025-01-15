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

public class MoveGuildTest : GuildTestBase
{
    private interface IMoveGuildFixture
    {
        public AgentAddress AgentAddress { get; }

        public FungibleAssetValue AgentNCG { get; }

        public GuildInfo GuildInfo1 { get; }

        public GuildInfo GuildInfo2 { get; }
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
        var action = new MoveGuild(guildAddress);
        var plainValue = action.PlainValue;

        var deserialized = new MoveGuild();
        deserialized.LoadPlainValue(plainValue);
        Assert.Equal(guildAddress, deserialized.GuildAddress);
    }

    [Fact]
    public void Execute()
    {
        var fixture = new StaticFixture
        {
            GuildInfo1 = new GuildInfo
            {
                ValidatorGG = GG * 100,
                SlashFactor = 0,
                GuildMasterNCG = NCG * 100,
            },
            GuildInfo2 = new GuildInfo
            {
                ValidatorGG = GG * 100,
                SlashFactor = 0,
                GuildMasterNCG = NCG * 100,
            },
            AgentNCG = NCG * 100,
        };
        ExecuteWithFixture(fixture);
    }

    [Fact]
    public void Execute_SlashedValidator()
    {
        var fixture = new StaticFixture
        {
            GuildInfo1 = new GuildInfo
            {
                ValidatorGG = GG * 100,
                SlashFactor = 10,
                GuildMasterNCG = NCG * 100,
            },
            GuildInfo2 = new GuildInfo
            {
                ValidatorGG = GG * 100,
                SlashFactor = 10,
                GuildMasterNCG = NCG * 100,
            },
            AgentNCG = NCG * 100,
        };
        ExecuteWithFixture(fixture);
    }

    [Fact]
    public void Execute_ToGuildDelegatingToTombstonedValidator_Throw()
    {
        // Given
        var world = World;
        var validatorKey1 = new PrivateKey();
        var validatorKey2 = new PrivateKey();
        var agentAddress = AddressUtil.CreateAgentAddress();
        var guildMasterAddress1 = AddressUtil.CreateAgentAddress();
        var guildMasterAddress2 = AddressUtil.CreateAgentAddress();
        var guildAddress1 = AddressUtil.CreateGuildAddress();
        var guildAddress2 = AddressUtil.CreateGuildAddress();
        world = EnsureToPrepareGuildGold(world, validatorKey1.Address, GG * 100);
        world = EnsureToCreateValidator(world, validatorKey1.PublicKey, GG * 100);
        world = EnsureToPrepareGuildGold(world, validatorKey2.Address, GG * 100);
        world = EnsureToCreateValidator(world, validatorKey2.PublicKey, GG * 100);
        world = EnsureToMakeGuild(world, guildAddress1, guildMasterAddress1, validatorKey1.Address);
        world = EnsureToMakeGuild(world, guildAddress2, guildMasterAddress2, validatorKey2.Address);
        world = EnsureToPrepareGuildGold(world, agentAddress, GG * 100);
        world = EnsureToJoinGuild(world, guildAddress1, agentAddress, 1L);
        world = EnsureToTombstoneValidator(world, validatorKey2.Address);

        // When
        var moveGuild = new MoveGuild(guildAddress2);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = agentAddress,
            BlockIndex = 2L,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => moveGuild.Execute(actionContext));
        Assert.Equal(
            "The validator of the guild to move to has been tombstoned.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1181126949)]
    [InlineData(793705868)]
    [InlineData(559431555)]
    [InlineData(1893396102)]
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

    private void ExecuteWithFixture(IMoveGuildFixture fixture)
    {
        // Given
        var world = World;
        var validatorKey1 = fixture.GuildInfo1.ValidatorKey;
        var validatorKey2 = fixture.GuildInfo2.ValidatorKey;
        var validatorGG1 = fixture.GuildInfo1.ValidatorGG;
        var validatorGG2 = fixture.GuildInfo2.ValidatorGG;
        var agentAddress = fixture.AgentAddress;
        var agentNCG = fixture.AgentNCG;
        var agentAmount = agentNCG.MajorUnit;
        var agentGG = NCGToGG(agentNCG);
        var guildMasterAddress1 = fixture.GuildInfo1.GuildMasterAddress;
        var guildMasterAddress2 = fixture.GuildInfo2.GuildMasterAddress;
        var guildMasterNCG1 = fixture.GuildInfo1.GuildMasterNCG;
        var guildMasterNCG2 = fixture.GuildInfo2.GuildMasterNCG;
        var guildMasterAmount1 = guildMasterNCG1.MajorUnit;
        var guildMasterAmount2 = guildMasterNCG2.MajorUnit;
        var guildMasterGG1 = NCGToGG(guildMasterNCG1);
        var guildMasterGG2 = NCGToGG(guildMasterNCG2);
        var guildAddress1 = fixture.GuildInfo1.GuildAddress;
        var guildAddress2 = fixture.GuildInfo2.GuildAddress;
        var height = 0L;
        var avatarIndex = 0;
        var slashFactor1 = fixture.GuildInfo1.SlashFactor;
        var slashFactor2 = fixture.GuildInfo2.SlashFactor;
        world = EnsureToPrepareGuildGold(world, validatorKey1.Address, validatorGG1);
        world = EnsureToCreateValidator(world, validatorKey1.PublicKey, validatorGG1);
        world = EnsureToPrepareGuildGold(world, validatorKey2.Address, validatorGG2);
        world = EnsureToCreateValidator(world, validatorKey2.PublicKey, validatorGG2);
        world = EnsureToMakeGuild(world, guildAddress1, guildMasterAddress1, validatorKey1.Address);
        world = EnsureToMakeGuild(world, guildAddress2, guildMasterAddress2, validatorKey2.Address);
        world = EnsureToMintAsset(world, guildMasterAddress1, guildMasterNCG1);
        world = EnsureToMintAsset(world, guildMasterAddress2, guildMasterNCG2);
        world = EnsureToCreateAvatar(world, guildMasterAddress1, avatarIndex);
        world = EnsureToCreateAvatar(world, guildMasterAddress2, avatarIndex);
        world = EnsureToStake(world, guildMasterAddress1, avatarIndex, guildMasterAmount1, height++);
        world = EnsureToStake(world, guildMasterAddress2, avatarIndex, guildMasterAmount2, height++);
        world = EnsureToMintAsset(world, agentAddress, agentNCG);
        world = EnsureToJoinGuild(world, guildAddress1, agentAddress, height++);
        world = EnsureToCreateAvatar(world, agentAddress, avatarIndex);
        world = EnsureToStake(world, agentAddress, avatarIndex, agentAmount, height++);
        if (slashFactor1 > 0)
        {
            world = EnsureToSlashValidator(world, validatorKey1.Address, slashFactor1, height++);
        }

        if (slashFactor2 > 0)
        {
            world = EnsureToSlashValidator(world, validatorKey2.Address, slashFactor2, height++);
        }

        // When
        var totalGG1 = validatorGG1 + guildMasterGG1 + agentGG;
        var totalGG2 = validatorGG2 + guildMasterGG2;
        var slashedGG1 = SlashFAV(slashFactor1, totalGG1);
        var slashedGG2 = SlashFAV(slashFactor2, totalGG2);
        var totalShare1 = totalGG1.RawValue;
        var totalShare2 = totalGG2.RawValue;
        var agentShare1 = totalShare1 * agentGG.RawValue / totalGG1.RawValue;
        var expectedAgengGG1 = (slashedGG1 * agentShare1).DivRem(totalShare1).Quotient;
        var agentShare2 = totalShare2 * expectedAgengGG1.RawValue / slashedGG2.RawValue;
        var expectedTotalGG1 = slashedGG1 - expectedAgengGG1;
        var expectedTotalGG2 = slashedGG2 + expectedAgengGG1;
        var expectedTotalShares1 = totalShare1 - agentShare1;
        var expectedTotalShares2 = totalShare2 + agentShare2;
        var moveGuild = new MoveGuild(guildAddress2);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = agentAddress,
            BlockIndex = height++,
        };
        world = moveGuild.Execute(actionContext);

        // Then
        var guildRepository = new GuildRepository(world, actionContext);
        var validatorRepository = new ValidatorRepository(world, actionContext);
        var guildDelegatee1 = guildRepository.GetDelegatee(validatorKey1.Address);
        var guildDelegatee2 = guildRepository.GetDelegatee(validatorKey2.Address);
        var validatorDelegatee1 = validatorRepository.GetDelegatee(validatorKey1.Address);
        var validatorDelegatee2 = validatorRepository.GetDelegatee(validatorKey2.Address);

        var guildParticipant = guildRepository.GetGuildParticipant(agentAddress);

        Assert.Equal(guildAddress2, guildParticipant.GuildAddress);
        Assert.Equal(expectedTotalGG1, guildDelegatee1.TotalDelegated);
        Assert.Equal(expectedTotalGG1, validatorDelegatee1.TotalDelegated);
        Assert.Equal(expectedTotalShares1, guildDelegatee1.TotalShares);
        Assert.Equal(expectedTotalShares1, validatorDelegatee1.TotalShares);
        Assert.Equal(expectedTotalGG2, guildDelegatee2.TotalDelegated);
        Assert.Equal(expectedTotalGG2, validatorDelegatee2.TotalDelegated);
        Assert.Equal(expectedTotalShares2, guildDelegatee2.TotalShares);
        Assert.Equal(expectedTotalShares2, validatorDelegatee2.TotalShares);
    }

    private struct GuildInfo
    {
        public GuildInfo()
        {
        }

        public GuildInfo(Random random)
        {
            ValidatorKey = GetRandomKey(random);
            ValidatorGG = GetRandomGG(random);
            SlashFactor = GetRandomSlashFactor(random);
            GuildAddress = GetRandomGuildAddress(random);
            GuildMasterAddress = GetRandomAgentAddress(random);
            GuildMasterNCG = GetRandomNCG(random);
        }

        public PrivateKey ValidatorKey { get; set; } = new PrivateKey();

        public FungibleAssetValue ValidatorGG { get; set; } = GG * 100;

        public BigInteger SlashFactor { get; set; } = 0;

        public GuildAddress GuildAddress { get; set; } = AddressUtil.CreateGuildAddress();

        public AgentAddress GuildMasterAddress { get; set; } = AddressUtil.CreateAgentAddress();

        public FungibleAssetValue GuildMasterNCG { get; set; } = NCG * 100;
    }

    private class StaticFixture : IMoveGuildFixture
    {
        public AgentAddress AgentAddress { get; set; } = AddressUtil.CreateAgentAddress();

        public FungibleAssetValue AgentNCG { get; set; } = NCG * 100;

        public GuildInfo GuildInfo1 { get; set; }

        public GuildInfo GuildInfo2 { get; set; }
    }

    private class RandomFixture : IMoveGuildFixture
    {
        private readonly Random _random;

        public RandomFixture(int randomSeed)
        {
            _random = new Random(randomSeed);
            AgentAddress = GetRandomAgentAddress(_random);
            AgentNCG = GetRandomNCG(_random);
            GuildInfo1 = new GuildInfo(_random);
            GuildInfo2 = new GuildInfo(_random);
        }

        public AgentAddress AgentAddress { get; }

        public FungibleAssetValue AgentNCG { get; }

        public GuildInfo GuildInfo1 { get; }

        public GuildInfo GuildInfo2 { get; }
    }
}
