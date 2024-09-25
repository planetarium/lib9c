#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Linq;
using System.Numerics;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume;
    using Nekoyume.Action.ValidatorDelegation;
    using Nekoyume.ValidatorDelegation;
using Xunit;

public class ClaimRewardValidatorTest : ValidatorDelegationTestBase
{
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
        var validatorGold = NCG * 10;
        var allocatedReward = NCG * 100;
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++, mint: true);
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
        var actualBalance = world.GetBalance(validatorKey.Address, NCG);

        Assert.Equal(expectedBalance, actualBalance);
    }

    [Theory]
    [InlineData(33.33)]
    [InlineData(11.11)]
    [InlineData(10)]
    [InlineData(1)]
    public void Execute_OneDelegator(double reward)
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var delegatorKey = new PrivateKey();
        var height = 1L;
        var actionContext = new ActionContext { };
        var promotedGold = NCG * 10;
        var allocatedReward = FungibleAssetValue.Parse(NCG, $"{reward:R}");
        world = EnsurePromotedValidator(world, validatorKey, promotedGold, height++, mint: true);
        world = EnsureBondedDelegator(world, delegatorKey, validatorKey, NCG * 10, height++);
        world = EnsureRewardAllocatedValidator(world, validatorKey, allocatedReward, ref height);

        // When
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetValidatorDelegatee(
            validatorKey.Address);
        var expectedCommission = GetCommission(
            allocatedReward, expectedDelegatee.CommissionPercentage);
        var expectedReward = allocatedReward - expectedCommission;
        var expectedValidatorBalance = expectedCommission + expectedReward.DivRem(2).Quotient;
        var expectedDelegatorBalance = expectedReward.DivRem(2).Quotient;
        var expectedRemainReward = allocatedReward;
        expectedRemainReward -= expectedValidatorBalance;
        expectedRemainReward -= expectedDelegatorBalance;

        var lastCommit = CreateLastCommit(validatorKey, height - 1);
        actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Signer = validatorKey.Address,
            LastCommit = lastCommit,
        };
        world = new ClaimRewardValidator(validatorKey.Address).Execute(actionContext);
        actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Signer = delegatorKey.Address,
            LastCommit = lastCommit,
        };
        world = new ClaimRewardValidator(validatorKey.Address).Execute(actionContext);

        // Then
        var repository = new ValidatorRepository(world, actionContext);
        var delegatee = repository.GetValidatorDelegatee(validatorKey.Address);
        var actualRemainReward = world.GetBalance(delegatee.CurrentLumpSumRewardsRecordAddress(), NCG);
        var actualValidatorBalance = world.GetBalance(validatorKey.Address, NCG);
        var actualDelegatorBalance = world.GetBalance(delegatorKey.Address, NCG);

        Assert.Equal(expectedRemainReward, actualRemainReward);
        Assert.Equal(expectedValidatorBalance, actualValidatorBalance);
        Assert.Equal(expectedDelegatorBalance, actualDelegatorBalance);
    }

    [Fact]
    public void Execute_TwoDelegators()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var delegatorKey1 = new PrivateKey();
        var delegatorKey2 = new PrivateKey();
        var height = 1L;
        var actionContext = new ActionContext { };
        var promotedGold = NCG * 10;
        var allocatedReward = FungibleAssetValue.Parse(NCG, $"{34.27:R}");
        world = EnsurePromotedValidator(world, validatorKey, promotedGold, height++, mint: true);
        world = EnsureBondedDelegator(world, delegatorKey1, validatorKey, NCG * 10, height++);
        world = EnsureBondedDelegator(world, delegatorKey2, validatorKey, NCG * 10, height++);
        world = EnsureRewardAllocatedValidator(world, validatorKey, allocatedReward, ref height);

        // When
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetValidatorDelegatee(validatorKey.Address);
        var expectedCommission = GetCommission(
            allocatedReward, expectedDelegatee.CommissionPercentage);
        var expectedReward = allocatedReward - expectedCommission;
        var expectedValidatorBalance = expectedCommission + expectedReward.DivRem(3).Quotient;
        var expectedDelegator1Balance = expectedReward.DivRem(3).Quotient;
        var expectedDelegator2Balance = expectedReward.DivRem(3).Quotient;
        var expectedRemainReward = expectedDelegatee.RewardCurrency * 0;
        var expectedCommunityBalance = allocatedReward;
        expectedCommunityBalance -= expectedValidatorBalance;
        expectedCommunityBalance -= expectedDelegator1Balance;
        expectedCommunityBalance -= expectedDelegator2Balance;

        var lastCommit = CreateLastCommit(validatorKey, height - 1);
        actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Signer = validatorKey.Address,
            LastCommit = lastCommit,
        };
        world = new ClaimRewardValidator(validatorKey.Address).Execute(actionContext);
        actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Signer = delegatorKey1.Address,
            LastCommit = lastCommit,
        };
        world = new ClaimRewardValidator(validatorKey.Address).Execute(actionContext);
        actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Signer = delegatorKey2.Address,
            LastCommit = lastCommit,
        };
        world = new ClaimRewardValidator(validatorKey.Address).Execute(actionContext);

        // Then
        var repository = new ValidatorRepository(world, actionContext);
        var delegatee = repository.GetValidatorDelegatee(validatorKey.Address);
        var actualRemainReward = world.GetBalance(
            delegatee.CurrentLumpSumRewardsRecordAddress(), NCG);
        var actualValidatorBalance = world.GetBalance(validatorKey.Address, NCG);
        var actualDelegator1Balance = world.GetBalance(delegatorKey1.Address, NCG);
        var actualDelegator2Balance = world.GetBalance(delegatorKey2.Address, NCG);

        Assert.Equal(expectedRemainReward, actualRemainReward);
        Assert.Equal(expectedValidatorBalance, actualValidatorBalance);
        Assert.Equal(expectedDelegator1Balance, actualDelegator1Balance);
        Assert.Equal(expectedDelegator2Balance, actualDelegator2Balance);
    }

    [Fact]
    public void Execute_MultipleDelegators()
    {
        // Given
        var length = Random.Shared.Next(3, 100);
        var world = World;
        var validatorKey = new PrivateKey();
        var delegatorKeys = GetRandomArray(length, _ => new PrivateKey());
        var delegatorNCGs = GetRandomArray(length, _ => GetRandomNCG());
        var height = 1L;
        var actionContext = new ActionContext();
        var promotedGold = GetRandomNCG();
        var allocatedReward = GetRandomNCG();
        world = EnsurePromotedValidator(world, validatorKey, promotedGold, height++, mint: true);
        world = EnsureBondedDelegators(world, delegatorKeys, validatorKey, delegatorNCGs, height++);
        world = EnsureRewardAllocatedValidator(world, validatorKey, allocatedReward, ref height);

        // When
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetValidatorDelegatee(validatorKey.Address);
        var expectedCommission = GetCommission(
            allocatedReward, expectedDelegatee.CommissionPercentage);
        var expectedReward = allocatedReward - expectedCommission;
        var expectedValidatorBalance = expectedCommission + CalculateReward(
            expectedRepository, validatorKey, validatorKey, expectedReward);
        var expectedDelegatorBalances = CalculateRewards(
            expectedRepository, validatorKey, delegatorKeys, expectedReward);
        var expectedCommunityBalance = allocatedReward;
        expectedCommunityBalance -= expectedValidatorBalance;
        for (var i = 0; i < length; i++)
        {
            expectedCommunityBalance -= expectedDelegatorBalances[i];
        }

        var expectedRemainReward = expectedDelegatee.RewardCurrency * 0;

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
        var actualRemainReward = world.GetBalance(
            delegatee.CurrentLumpSumRewardsRecordAddress(), NCG);
        var actualValidatorBalance = world.GetBalance(validatorKey.Address, NCG);
        var actualCommunityBalance = world.GetBalance(Addresses.CommunityPool, NCG);
        Assert.Equal(expectedRemainReward, actualRemainReward);
        Assert.Equal(expectedValidatorBalance, actualValidatorBalance);
        Assert.Equal(expectedCommunityBalance, actualCommunityBalance);

        for (var i = 0; i < length; i++)
        {
            var actualDelegatorBalance = world.GetBalance(delegatorKeys[i].Address, NCG);
            Assert.Equal(expectedDelegatorBalances[i], actualDelegatorBalance);
        }
    }

    private static FungibleAssetValue CalculateReward(
        ValidatorRepository repository,
        PrivateKey validatorKey,
        PrivateKey delegatorKey,
        FungibleAssetValue reward)
    {
        var delegatee = repository.GetValidatorDelegatee(validatorKey.Address);
        var bond = repository.GetBond(delegatee, delegatorKey.Address);
        return CalculateReward(reward, bond.Share, delegatee.TotalShares);
    }

    private static FungibleAssetValue[] CalculateRewards(
        ValidatorRepository repository,
        PrivateKey validatorKey,
        PrivateKey[] delegatorKeys,
        FungibleAssetValue reward)
    {
        return delegatorKeys
            .Select(item => CalculateReward(repository, validatorKey, item, reward))
            .ToArray();
    }

    private static FungibleAssetValue CalculateReward(
        FungibleAssetValue reward, BigInteger share, BigInteger totalShares)
    {
        return (reward * share).DivRem(totalShares).Quotient;
    }
}
