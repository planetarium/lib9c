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
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class RemoveGuildTest : GuildTestBase
{
    private interface IRemoveGuildFixture
    {
        public PrivateKey ValidatorKey { get; }

        public FungibleAssetValue ValidatorNCG { get; }

        public BigInteger SlashFactor { get; }

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
        var action = new RemoveGuild();
        var plainValue = action.PlainValue;

        var deserialized = new RemoveGuild();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        var fixture = new StaticFixture
        {
            ValidatorNCG = NCG * 100,
            SlashFactor = 0,
            GuildMasterNCG = NCG * 100,
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
            GuildMasterNCG = NCG * 100,
        };

        ExecuteWithFixture(fixture);
    }

    [Fact]
    public void Execute_UnknownGuild_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToSetGuildParticipant(world, guildMasterAddress, guildAddress);

        // When
        var removeGuild = new RemoveGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => removeGuild.Execute(actionContext));
        Assert.Equal("There is no such guild.", exception.Message);
    }

    [Fact]
    public void Execute_ByGuildMember_Throw()
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
        var removeGuild = new RemoveGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMemberAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => removeGuild.Execute(actionContext));
        Assert.Equal("The signer is not a guild master.", exception.Message);
    }

    [Fact]
    public void Execute_GuildHasMember_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildParticipantAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToPrepareGuildGold(world, validatorKey.Address, GG * 100);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, GG * 100);
        world = EnsureToPrepareGuildGold(world, guildMasterAddress, GG * 100);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToJoinGuild(world, guildAddress, guildParticipantAddress, 1L);

        // When
        var removeGuild = new RemoveGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => removeGuild.Execute(actionContext));
        Assert.Equal("There are remained participants in the guild.", exception.Message);
    }

    [Fact]
    public void Execute_GuildHasBond_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToPrepareGuildGold(world, validatorKey.Address, GG * 100);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, GG * 100);
        world = EnsureToPrepareGuildGold(world, guildMasterAddress, GG * 100);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);

        // When
        var removeGuild = new RemoveGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => removeGuild.Execute(actionContext));
        Assert.Equal("The signer has a bond with the validator.", exception.Message);
    }

    [Fact]
    public void Execute_ByNonGuildMember_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var otherAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        world = EnsureToPrepareGuildGold(world, validatorKey.Address, GG * 100);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, GG * 100);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);

        // When
        var removeGuild = new RemoveGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = otherAddress,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => removeGuild.Execute(actionContext));
        Assert.Equal("The signer does not join any guild.", exception.Message);
    }

    [Fact]
    public void Execute_ResetBannedAddresses()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var guildMasterAddress = AddressUtil.CreateAgentAddress();
        var guildAddress = AddressUtil.CreateGuildAddress();
        var bannedAddress = AddressUtil.CreateAgentAddress();
        world = EnsureToPrepareGuildGold(world, validatorKey.Address, GG * 100);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, GG * 100);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToJoinGuild(world, guildAddress, bannedAddress, 1L);
        world = EnsureToBanGuildMember(world, guildMasterAddress, bannedAddress);

        // When
        var removeGuild = new RemoveGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
        };
        world = removeGuild.Execute(actionContext);

        // Then
        var repository = new GuildRepository(world, actionContext);
        Assert.False(repository.IsBanned(guildAddress, bannedAddress));
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

    private void ExecuteWithFixture(IRemoveGuildFixture fixture)
    {
        // Given
        var world = World;
        var validatorKey = fixture.ValidatorKey;
        var validatorNCG = fixture.ValidatorNCG;
        var validatorAmount = validatorNCG.MajorUnit;
        var validatorGG = NCGToGG(validatorNCG);
        var guildMasterAddress = fixture.GuildMasterAddress;
        var guildMasterNCG = fixture.GuildMasterNCG;
        var guildMasterAmount = guildMasterNCG.MajorUnit;
        var guildMasterGG = NCGToGG(guildMasterNCG);
        var guildAddress = fixture.GuildAddress;
        var height = 0L;
        var avatarIndex = 0;
        var slashFactor = fixture.SlashFactor;
        world = EnsureToMintAsset(world, validatorKey.Address, validatorNCG);
        world = EnsureToStake(world, validatorKey.Address, validatorAmount, height++);
        world = EnsureToCreateValidator(world, validatorKey.PublicKey, validatorGG);
        if (slashFactor > 0)
        {
            world = EnsureToSlashValidator(world, validatorKey.Address, slashFactor, height++);
        }

        world = EnsureToMintAsset(world, guildMasterAddress, guildMasterNCG);
        world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
        world = EnsureToCreateAvatar(world, guildMasterAddress, avatarIndex);
        world = EnsureToStake(world, guildMasterAddress, avatarIndex, guildMasterAmount, height++);
        world = EnsureToStake(world, guildMasterAddress, avatarIndex, amount: 0, height++);

        // When
        var totalGG = validatorGG;
        var slashedGG = SlashFAV(slashFactor, totalGG);
        var totalShare = totalGG.RawValue;
        var expectedTotalGG = slashedGG;
        var expectedTotalShares = totalGG.RawValue;
        var removeGuild = new RemoveGuild();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = guildMasterAddress,
            BlockIndex = height,
        };
        world = removeGuild.Execute(actionContext);

        // Then
        var guildRepository = new GuildRepository(world, actionContext);
        var validatorRepository = new ValidatorRepository(world, actionContext);
        var guildDelegatee = guildRepository.GetDelegatee(validatorKey.Address);
        var validatorDelegatee = validatorRepository.GetDelegatee(validatorKey.Address);
        var comparer = new FungibleAssetValueEqualityComparer(GGEpsilon);

        Assert.Throws<FailedLoadStateException>(() => guildRepository.GetGuild(guildAddress));
        Assert.Equal(expectedTotalGG, guildDelegatee.TotalDelegated, comparer);
        Assert.Equal(expectedTotalGG, validatorDelegatee.TotalDelegated, comparer);
        Assert.Equal(expectedTotalShares, guildDelegatee.TotalShares);
        Assert.Equal(expectedTotalShares, validatorDelegatee.TotalShares);
    }

    private class StaticFixture : IRemoveGuildFixture
    {
        public PrivateKey ValidatorKey { get; set; } = new PrivateKey();

        public FungibleAssetValue ValidatorNCG { get; set; } = GG * 100;

        public BigInteger SlashFactor { get; set; }

        public GuildAddress GuildAddress { get; set; } = AddressUtil.CreateGuildAddress();

        public AgentAddress GuildMasterAddress { get; set; } = AddressUtil.CreateAgentAddress();

        public FungibleAssetValue GuildMasterNCG { get; set; } = NCG * 100;
    }

    private class RandomFixture : IRemoveGuildFixture
    {
        private readonly Random _random;

        public RandomFixture(int randomSeed)
        {
            _random = new Random(randomSeed);
            ValidatorKey = GetRandomKey(_random);
            ValidatorNCG = GetRandomNCG(_random);
            SlashFactor = GetRandomSlashFactor(_random);
            GuildAddress = GetRandomGuildAddress(_random);
            GuildMasterAddress = GetRandomAgentAddress(_random);
            GuildMasterNCG = GetRandomNCG(_random);
        }

        public PrivateKey ValidatorKey { get; }

        public FungibleAssetValue ValidatorNCG { get; }

        public BigInteger SlashFactor { get; }

        public GuildAddress GuildAddress { get; }

        public AgentAddress GuildMasterAddress { get; }

        public FungibleAssetValue GuildMasterNCG { get; }
    }
}
