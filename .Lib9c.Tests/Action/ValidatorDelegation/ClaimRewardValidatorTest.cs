#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class ClaimRewardValidatorTest : ValidatorDelegationTestBase
{
    private interface IClaimRewardFixture
    {
        FungibleAssetValue TotalReward { get; }

        PrivateKey ValidatorKey { get; }

        FungibleAssetValue ValidatorBalance { get; }

        FungibleAssetValue ValidatorCash { get; }

        DelegatorInfo[] DelegatorInfos { get; }

        PrivateKey[] DelegatorKeys => DelegatorInfos.Select(i => i.Key).ToArray();

        FungibleAssetValue[] DelegatorBalances => DelegatorInfos.Select(i => i.Balance).ToArray();

        FungibleAssetValue[] DelegatorCashes => DelegatorInfos.Select(i => i.Cash).ToArray();
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
        var action = new ClaimRewardValidator();
        var plainValue = action.PlainValue;

        var deserialized = new ClaimRewardValidator();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        var validatorGold = GG * 10;
        var allocatedReward = GG * 100;
        world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
        world = EnsureRewardAllocatedValidator(world, validatorKey, allocatedReward, ref height);

        // When
        var expectedBalance = allocatedReward;
        var lastCommit = CreateLastCommit(validatorKey, height - 1);
        var claimRewardValidator = new ClaimRewardValidator(validatorKey.Address);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Signer = validatorKey.Address,
            LastCommit = lastCommit,
        };
        world = claimRewardValidator.Execute(actionContext);

        // Then
        var actualBalance = world.GetBalance(validatorKey.Address, GG);

        Assert.Equal(expectedBalance, actualBalance);
    }

    [Theory]
    [InlineData(33.33)]
    [InlineData(11.11)]
    [InlineData(10)]
    [InlineData(1)]
    public void Execute_Theory_OneDelegator(decimal totalReward)
    {
        var fixture = new StaticFixture
        {
            DelegatorLength = 1,
            TotalReward = FungibleAssetValue.Parse(GG, $"{totalReward}"),
            ValidatorKey = new PrivateKey(),
            ValidatorBalance = GG * 100,
            ValidatorCash = GG * 10,
            DelegatorInfos = new[]
            {
                new DelegatorInfo
                {
                    Key = new PrivateKey(),
                    Balance = GG * 100,
                    Cash = GG * 10,
                },
            },
        };
        ExecuteWithFixture(fixture);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(9)]
    [InlineData(11.11)]
    [InlineData(11.12)]
    [InlineData(33.33)]
    [InlineData(33.34)]
    [InlineData(34.27)]
    [InlineData(34.28)]
    [InlineData(34.29)]
    public void Execute_Theory_TwoDelegators(decimal totalReward)
    {
        var fixture = new StaticFixture
        {
            DelegatorLength = 2,
            TotalReward = FungibleAssetValue.Parse(GG, $"{totalReward}"),
            ValidatorKey = new PrivateKey(),
            ValidatorBalance = GG * 100,
            ValidatorCash = GG * 10,
            DelegatorInfos = new[]
            {
                new DelegatorInfo
                {
                    Key = new PrivateKey(),
                    Balance = GG * 100,
                    Cash = GG * 10,
                },
                new DelegatorInfo
                {
                    Key = new PrivateKey(),
                    Balance = GG * 100,
                    Cash = GG * 10,
                },
            },
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
        var delegatorBalances = fixture.DelegatorBalances;
        var delegatorCashes = fixture.DelegatorCashes;
        var height = 1L;
        var actionContext = new ActionContext();
        var validatorBalance = fixture.ValidatorBalance;
        var validatorCash = fixture.ValidatorCash;
        var totalReward = fixture.TotalReward;
        world = EnsureToMintAsset(world, validatorKey, validatorBalance, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorCash, height++);
        world = EnsureToMintAssets(world, delegatorKeys, delegatorBalances, height++);
        world = EnsureBondedDelegators(
            world, delegatorKeys, validatorKey, delegatorCashes, height++);
        world = EnsureRewardAllocatedValidator(world, validatorKey, totalReward, ref height);

        // Calculate expected values for comparison with actual values.
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetValidatorDelegatee(validatorKey.Address);
        var expectedTotalShares = expectedDelegatee.TotalShares;
        var expectedValidatorShare
            = expectedRepository.GetBond(expectedDelegatee, validatorKey.Address).Share;
        var expectedDelegatorShares = delegatorKeys
            .Select(item => expectedRepository.GetBond(expectedDelegatee, item.Address).Share)
            .ToArray();
        var expectedProposerReward
            = CalculatePropserReward(totalReward) + CalculateBonusPropserReward(1, 1, totalReward);
        var expectedReward = totalReward - expectedProposerReward;
        var expectedCommission = CalculateCommission(
            expectedReward, expectedDelegatee.CommissionPercentage);
        var expectedClaim = expectedReward - expectedCommission;
        var expectedValidatorClaim = CalculateClaim(
            expectedValidatorShare, expectedTotalShares, expectedClaim);
        var expectedDelegatorClaims = CreateArray(
            length,
            i => CalculateClaim(expectedDelegatorShares[i], expectedTotalShares, expectedClaim));
        var expectedValidatorBalance = validatorBalance;
        expectedValidatorBalance -= validatorCash;
        expectedValidatorBalance += expectedProposerReward;
        expectedValidatorBalance += expectedCommission;
        expectedValidatorBalance += expectedValidatorClaim;
        var expectedDelegatorBalances = CreateArray(
            length,
            i => delegatorBalances[i] - delegatorCashes[i] + expectedDelegatorClaims[i]);
        var expectedRemainReward = totalReward;
        expectedRemainReward -= expectedProposerReward;
        expectedRemainReward -= expectedCommission;
        expectedRemainReward -= expectedValidatorClaim;
        for (var i = 0; i < length; i++)
        {
            expectedRemainReward -= expectedDelegatorClaims[i];
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
        world = new ClaimRewardValidator(validatorKey.Address).Execute(actionContext);
        for (var i = 0; i < length; i++)
        {
            actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = height++,
                Signer = delegatorKeys[i].Address,
                LastCommit = lastCommit,
            };
            world = new ClaimRewardValidator(validatorKey.Address).Execute(actionContext);
        }

        // Then
        var repository = new ValidatorRepository(world, actionContext);
        var delegatee = repository.GetValidatorDelegatee(validatorKey.Address);
        var actualRemainReward = world.GetBalance(delegatee.RewardRemainderPoolAddress, GG);
        var actualValidatorBalance = world.GetBalance(validatorKey.Address, GG);
        var actualDelegatorBalances = delegatorKeys
            .Select(item => world.GetBalance(item.Address, GG))
            .ToArray();
        Assert.Equal(expectedRemainReward, actualRemainReward);
        Assert.Equal(expectedValidatorBalance, actualValidatorBalance);
        Assert.Equal(expectedDelegatorBalances, actualDelegatorBalances);
    }

    private struct DelegatorInfo
    {
        public PrivateKey Key { get; set; }

        public FungibleAssetValue Cash { get; set; }

        public FungibleAssetValue Balance { get; set; }
    }

    private struct StaticFixture : IClaimRewardFixture
    {
        public int DelegatorLength { get; set; }

        public FungibleAssetValue TotalReward { get; set; }

        public PrivateKey ValidatorKey { get; set; }

        public FungibleAssetValue ValidatorBalance { get; set; }

        public FungibleAssetValue ValidatorCash { get; set; }

        public DelegatorInfo[] DelegatorInfos { get; set; }
    }

    private class RandomFixture : IClaimRewardFixture
    {
        private readonly Random _random;

        public RandomFixture(int randomSeed)
        {
            _random = new Random(randomSeed);
            DelegatorLength = _random.Next(3, 100);
            ValidatorKey = new PrivateKey();
            TotalReward = GetRandomGG(_random);
            ValidatorBalance = GetRandomGG(_random);
            ValidatorCash = GetRandomCash(_random, ValidatorBalance);
            DelegatorInfos = CreateArray(DelegatorLength, _ =>
            {
                var balance = GetRandomGG(_random);
                return new DelegatorInfo
                {
                    Key = new PrivateKey(),
                    Balance = balance,
                    Cash = GetRandomCash(_random, balance),
                };
            });
        }

        public int DelegatorLength { get; }

        public FungibleAssetValue TotalReward { get; }

        public PrivateKey ValidatorKey { get; }

        public FungibleAssetValue ValidatorBalance { get; }

        public FungibleAssetValue ValidatorCash { get; }

        public DelegatorInfo[] DelegatorInfos { get; }
    }
}
