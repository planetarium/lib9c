#nullable enable
namespace Lib9c.Tests.Action.Guild;

using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Tests.Action.ValidatorDelegation;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Guild;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.Model.Guild;
using Nekoyume.Model.Stake;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class ClaimRewardTest : ValidatorDelegationTestBase
{
    private interface IClaimRewardFixture
    {
        FungibleAssetValue TotalGuildAllocateReward { get; }

        FungibleAssetValue TotalAllocateReward { get; }

        PrivateKey ValidatorKey { get; }

        FungibleAssetValue ValidatorBalance { get; }

        FungibleAssetValue ValidatorCash { get; }

        DelegatorInfo[] DelegatorInfos { get; }

        GuildParticipantInfo[] GuildParticipantInfos { get; }

        PrivateKey[] DelegatorKeys => DelegatorInfos.Select(i => i.Key).ToArray();

        PrivateKey[] GuildParticipantKeys => GuildParticipantInfos.Select(i => i.Key).ToArray();

        FungibleAssetValue[] DelegatorBalances => DelegatorInfos.Select(i => i.Balance).ToArray();

        FungibleAssetValue[] GuildParticipantBalances => GuildParticipantInfos.Select(i => i.Balance).ToArray();
    }

    public static IEnumerable<object[]> RandomSeeds => new List<object[]>
    {
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
    };

    [Fact]
    public void Serialization()
    {
        var action = new ClaimGuildReward();
        var plainValue = action.PlainValue;

        var deserialized = new ClaimGuildReward();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        var validatorGold = DelegationCurrency * 10;
        var allocatedGuildRewards = GuildAllocateRewardCurrency * 100;
        var allocatedReward = AllocateRewardCurrency * 100;
        world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
        world = EnsureRewardAllocatedValidator(world, validatorKey, allocatedGuildRewards, allocatedReward, ref height);

        // When
        var expectedBalance = allocatedGuildRewards;
        var lastCommit = CreateLastCommit(validatorKey, height - 1);
        var claimRewardValidator = new ClaimValidatorRewardSelf();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Signer = validatorKey.Address,
            LastCommit = lastCommit,
        };
        world = claimRewardValidator.Execute(actionContext);

        // Then
        var actualBalance = world.GetBalance(validatorKey.Address, GuildAllocateRewardCurrency);

        Assert.Equal(expectedBalance, actualBalance);
    }

    [Theory]
    [InlineData(33.33, 33.33)]
    [InlineData(11.11, 11.11)]
    [InlineData(10, 10)]
    [InlineData(1, 1)]
    public void Execute_Theory_OneDelegator(
        decimal totalGuildReward,
        decimal totalReward)
    {
        var delegatorInfos = new[]
        {
            new DelegatorInfo
            {
                Key = new PrivateKey(),
                Balance = DelegationCurrency * 100,
            },
        };

        var guildParticipantInfos = new[]
        {
            new GuildParticipantInfo
            {
                Key = new PrivateKey(),
                Balance = DelegationCurrency * 100,
                GuildMasterAddress = delegatorInfos[0].Key.Address,
            },
        };

        var fixture = new StaticFixture
        {
            DelegatorLength = 1,
            TotalGuildAllocateReward = FungibleAssetValue.Parse(GuildAllocateRewardCurrency, $"{totalGuildReward}"),
            TotalAllocateReward = FungibleAssetValue.Parse(RewardCurrency, $"{totalReward}"),
            ValidatorKey = new PrivateKey(),
            ValidatorBalance = DelegationCurrency * 100,
            ValidatorCash = DelegationCurrency * 10,
            DelegatorInfos = delegatorInfos,
            GuildParticipantInfos = guildParticipantInfos,
        };
        ExecuteWithFixture(fixture);
    }

    [Theory]
    [InlineData(0.1, 0.1)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(5, 5)]
    [InlineData(7, 7)]
    [InlineData(9, 9)]
    [InlineData(11.11, 11.11)]
    [InlineData(11.12, 11.12)]
    [InlineData(33.33, 33.33)]
    [InlineData(33.34, 33.34)]
    [InlineData(34.27, 34.27)]
    [InlineData(34.28, 34.28)]
    [InlineData(34.29, 34.29)]
    public void Execute_Theory_TwoDelegators(
        decimal totalGuildReward,
        decimal totalReward)
    {
        var delegatorInfos = new[]
        {
            new DelegatorInfo
            {
                Key = new PrivateKey(),
                Balance = DelegationCurrency * 100,
            },
            new DelegatorInfo
            {
                Key = new PrivateKey(),
                Balance = DelegationCurrency * 100,
            },
        };

        var guildParticipantInfos = new[]
        {
            new GuildParticipantInfo
            {
                Key = new PrivateKey(),
                Balance = DelegationCurrency * 100,
                GuildMasterAddress = delegatorInfos[0].Key.Address,
            },
            new GuildParticipantInfo
            {
                Key = new PrivateKey(),
                Balance = DelegationCurrency * 100,
                GuildMasterAddress = delegatorInfos[1].Key.Address,
            },
        };

        var fixture = new StaticFixture
        {
            DelegatorLength = 2,
            TotalGuildAllocateReward = FungibleAssetValue.Parse(GuildAllocateRewardCurrency, $"{totalGuildReward}"),
            TotalAllocateReward = FungibleAssetValue.Parse(AllocateRewardCurrency, $"{totalReward}"),
            ValidatorKey = new PrivateKey(),
            ValidatorBalance = DelegationCurrency * 100,
            ValidatorCash = DelegationCurrency * 10,
            DelegatorInfos = delegatorInfos,
            GuildParticipantInfos = guildParticipantInfos,
        };
        ExecuteWithFixture(fixture);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(123)]
    [InlineData(34352535)]
    public void Execute_Theory_WithStaticSeed(int randomSeed)
    {
        var fixture = new RandomFixture(randomSeed);
        ExecuteWithFixture(fixture);
    }

    [Theory]
    [MemberData(nameof(RandomSeeds))]
    public void Execute_Theory_WithRandomSeed(int randomSeed)
    {
        var fixture = new RandomFixture(randomSeed);
        ExecuteWithFixture(fixture);
    }

    private void ExecuteWithFixture(IClaimRewardFixture fixture)
    {
        // Given
        var length = fixture.DelegatorInfos.Length;
        var world = World;
        var validatorKey = fixture.ValidatorKey;
        var delegatorKeys = fixture.DelegatorKeys;
        var guildParticipantInfos = fixture.GuildParticipantInfos;
        var guildParticipantKeys = fixture.GuildParticipantKeys;
        var delegatorBalances = fixture.DelegatorBalances;
        var height = 1L;
        var actionContext = new ActionContext();
        var validatorBalance = fixture.ValidatorBalance;
        var validatorCash = fixture.ValidatorCash;
        var totalGuildReward = fixture.TotalGuildAllocateReward;
        var totalReward = fixture.TotalAllocateReward;
        int seed = 0;
        world = EnsureToMintAsset(world, validatorKey, validatorBalance, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorCash, height++);
        world = EnsureToMintAssets(world, delegatorKeys, delegatorBalances, height++);
        world = delegatorKeys.Aggregate(world, (w, d) => EnsureMakeGuild(
                w, d.Address, validatorKey.Address, height++, seed++));
        world = guildParticipantInfos.Aggregate(world, (w, i) => EnsureJoinGuild(
                w, i.Key.Address, i.GuildMasterAddress, validatorKey.Address, height++));

        world = EnsureRewardAllocatedValidator(world, validatorKey, totalGuildReward, totalReward, ref height);

        // Calculate expected values for comparison with actual values.
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedGuildRepository = new GuildRepository(expectedRepository);
        var expectedDelegatee = expectedRepository.GetValidatorDelegatee(validatorKey.Address);
        var expectedTotalShares = expectedDelegatee.TotalShares;
        var expectedValidatorShare
            = expectedRepository.GetBond(expectedDelegatee, validatorKey.Address).Share;
        var expectedDelegatorShares = delegatorKeys
            .Select(item => expectedRepository.GetBond(
                expectedDelegatee,
                (Address)expectedGuildRepository.GetJoinedGuild(new AgentAddress(item.Address))!).Share)
            .ToArray();
        var expectedProposerReward
            = CalculatePropserReward(totalGuildReward) + CalculateBonusPropserReward(1, 1, totalGuildReward);
        var expectedGuildReward = totalGuildReward - expectedProposerReward;
        var expectedCommission = CalculateCommission(
            expectedGuildReward, expectedDelegatee.CommissionPercentage);
        var expectedGuildClaim = expectedGuildReward - expectedCommission;
        var expectedValidatorGuildClaim = CalculateClaim(
            expectedValidatorShare, expectedTotalShares, expectedGuildClaim);
        var expectedDelegatorGuildClaims = CreateArray(
            length,
            i => CalculateClaim(expectedDelegatorShares[i], expectedTotalShares, expectedGuildClaim));
        var expectedValidatorBalance = validatorBalance;
        expectedValidatorBalance -= validatorCash;
        var expectedValidatorGuildReward = expectedProposerReward;
        expectedValidatorGuildReward += expectedCommission;
        expectedValidatorGuildReward += expectedValidatorGuildClaim;
        var expectedDelegatorBalances = CreateArray(length, i => DelegationCurrency * 0);
        var expectedRemainGuildReward = totalGuildReward;
        expectedRemainGuildReward -= expectedProposerReward;
        expectedRemainGuildReward -= expectedCommission;
        expectedRemainGuildReward -= expectedValidatorGuildClaim;
        for (var i = 0; i < length; i++)
        {
            expectedRemainGuildReward -= expectedDelegatorGuildClaims[i];
        }

        var expectedValidatorReward = totalReward.DivRem(10).Quotient;
        var expectedValidatorClaim = expectedValidatorReward;
        var expectedTotalGuildRewards = (totalReward - expectedValidatorReward).DivRem(10).Quotient;
        var expectedTotalGuildParticipantRewards = totalReward - expectedValidatorReward - expectedTotalGuildRewards;
        var expectedGuildClaims = CreateArray(
            length,
            i => CalculateClaim(expectedDelegatorShares[i], expectedTotalShares, expectedTotalGuildRewards));
        expectedValidatorClaim += CalculateClaim(expectedValidatorShare, expectedTotalShares, expectedTotalGuildRewards);
        var expectedGuildParticipantClaims = CreateArray(
            length,
            i => CalculateClaim(expectedDelegatorShares[i], expectedTotalShares, expectedTotalGuildParticipantRewards));
        expectedValidatorClaim += CalculateClaim(expectedValidatorShare, expectedTotalShares, expectedTotalGuildParticipantRewards);
        var expectedRemainReward = totalReward;
        expectedRemainReward -= expectedValidatorClaim;
        for (var i = 0; i < length; i++)
        {
            expectedRemainReward -= expectedGuildClaims[i];
        }

        for (var i = 0; i < length; i++)
        {
            expectedRemainReward -= expectedGuildParticipantClaims[i];
        }

        // When
        var lastCommit = CreateLastCommit(validatorKey, height - 1);
        actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Signer = validatorKey.Address,
            LastCommit = lastCommit,
        };
        world = new ClaimValidatorRewardSelf().Execute(actionContext);

        for (var i = 0; i < length; i++)
        {
            actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = height++,
                Signer = delegatorKeys[i].Address,
                LastCommit = lastCommit,
            };
            world = new ClaimGuildReward().Execute(actionContext);
        }

        for (var i = 0; i < length; i++)
        {
            actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = height++,
                Signer = delegatorKeys[i].Address,
                LastCommit = lastCommit,
            };
            world = new ClaimReward().Execute(actionContext);
        }

        // Then
        var validatorRepository = new ValidatorRepository(world, actionContext);
        var guildRepository = new GuildRepository(world, actionContext);
        var delegatee = validatorRepository.GetValidatorDelegatee(validatorKey.Address);
        var actualRemainGuildReward = world.GetBalance(delegatee.RewardRemainderPoolAddress, GuildAllocateRewardCurrency);
        var actualValidatorBalance = world.GetBalance(StakeState.DeriveAddress(validatorKey.Address), DelegationCurrency);
        var actualValidatorGuildReward = world.GetBalance(validatorKey.Address, GuildAllocateRewardCurrency);
        var actualDelegatorBalances = delegatorKeys
            .Select(item => world.GetBalance(item.Address, DelegationCurrency))
            .ToArray();
        var actualDelegatorGuildRewards = delegatorKeys
            .Select(item => world.GetBalance(
                guildRepository.GetJoinedGuild(
                    new AgentAddress(item.Address))
                ?? throw new Exception($"Delegator {item.Address} does not joind to guild."),
                GuildAllocateRewardCurrency))
            .ToArray();

        var actualRemainReward = world.GetBalance(delegatee.RewardRemainderPoolAddress, AllocateRewardCurrency);
        var actualValidatorReward = world.GetBalance(validatorKey.Address, AllocateRewardCurrency);
        var actualGuildRewards = delegatorKeys
            .Select(item => world.GetBalance(
                guildRepository.GetJoinedGuild(
                    new AgentAddress(item.Address))
                ?? throw new Exception($"Delegator {item.Address} does not joind to guild."),
                AllocateRewardCurrency))
            .ToArray();
        var actualGuildParticipantRewards = delegatorKeys
            .Select(item => world.GetBalance(
                item.Address,
                AllocateRewardCurrency))
            .ToArray();

        Assert.Equal(expectedValidatorBalance, actualValidatorBalance);
        Assert.Equal(expectedDelegatorBalances, actualDelegatorBalances);
        Assert.Equal(expectedValidatorGuildReward, actualValidatorGuildReward);
        Assert.Equal(expectedDelegatorGuildClaims, actualDelegatorGuildRewards);
        Assert.Equal(expectedValidatorClaim, actualValidatorReward);
        Assert.Equal(expectedGuildClaims, actualGuildRewards);
        Assert.Equal(expectedGuildParticipantClaims, actualGuildParticipantRewards);
        // Flushing to remainder pool is now inactive.
        // Assert.Equal(expectedRemainGuildReward, actualRemainGuildReward);
        // Assert.Equal(expectedRemainReward, actualRemainReward);
        foreach (var key in guildParticipantKeys)
        {
            Assert.Throws<InvalidOperationException>(
                () => new ClaimGuildReward().Execute(new ActionContext
                {
                    PreviousState = world,
                    BlockIndex = height++,
                    Signer = key.Address,
                    LastCommit = lastCommit,
                }));
        }
    }

    private struct DelegatorInfo
    {
        public PrivateKey Key { get; set; }

        public FungibleAssetValue Balance { get; set; }
    }

    private struct GuildParticipantInfo
    {
        public PrivateKey Key { get; set; }

        public FungibleAssetValue Balance { get; set; }

        public Address GuildMasterAddress { get; set; }
    }

    private struct StaticFixture : IClaimRewardFixture
    {
        public int DelegatorLength { get; set; }

        public FungibleAssetValue TotalGuildAllocateReward { get; set; }

        public FungibleAssetValue TotalAllocateReward { get; set; }

        public PrivateKey ValidatorKey { get; set; }

        public FungibleAssetValue ValidatorBalance { get; set; }

        public FungibleAssetValue ValidatorCash { get; set; }

        public DelegatorInfo[] DelegatorInfos { get; set; }

        public GuildParticipantInfo[] GuildParticipantInfos { get; set; }
    }

    private class RandomFixture : IClaimRewardFixture
    {
        private readonly Random _random;

        public RandomFixture(int randomSeed)
        {
            _random = new Random(randomSeed);
            DelegatorLength = _random.Next(3, 100);
            GuildParticipantLength = _random.Next(1, 50);
            ValidatorKey = new PrivateKey();
            TotalGuildAllocateReward = GetRandomFAV(GuildAllocateRewardCurrency, _random);
            TotalAllocateReward = GetRandomFAV(AllocateRewardCurrency, _random);
            ValidatorBalance = GetRandomFAV(DelegationCurrency, _random);
            ValidatorCash = GetRandomCash(_random, ValidatorBalance);
            DelegatorInfos = CreateArray(DelegatorLength, _ =>
            {
                var balance = GetRandomFAV(DelegationCurrency, _random);
                return new DelegatorInfo
                {
                    Key = new PrivateKey(),
                    Balance = balance,
                };
            });
            GuildParticipantInfos = CreateArray(GuildParticipantLength, _ =>
            {
                var balance = GetRandomFAV(DelegationCurrency, _random);
                return new GuildParticipantInfo
                {
                    Key = new PrivateKey(),
                    Balance = balance,
                    GuildMasterAddress = DelegatorInfos[_random.Next(DelegatorLength)].Key.Address,
                };
            });
        }

        public int DelegatorLength { get; }

        public int GuildParticipantLength { get; }

        public FungibleAssetValue TotalGuildAllocateReward { get; }

        public FungibleAssetValue TotalAllocateReward { get; }

        public PrivateKey ValidatorKey { get; }

        public FungibleAssetValue ValidatorBalance { get; }

        public FungibleAssetValue ValidatorCash { get; }

        public DelegatorInfo[] DelegatorInfos { get; }

        public GuildParticipantInfo[] GuildParticipantInfos { get; }
    }
}
