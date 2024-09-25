#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation
{
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
            var validatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            ActionContext actionContext;
            var validatorGold = NCG * 10;
            var allocatedReward = NCG * 100;
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, validatorGold, blockHeight++);
            world = EnsureValidatorToBeAllocatedReward(
                world, validatorPrivateKey, allocatedReward, ref blockHeight);

            // When
            var expectedBalance = allocatedReward;
            var lastCommit = CreateLastCommit(validatorPrivateKey, blockHeight - 1);
            var claimRewardValidator = new ClaimRewardValidator(validatorPrivateKey.Address);
            actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight++,
                Signer = validatorPrivateKey.Address,
                LastCommit = lastCommit,
            };
            world = claimRewardValidator.Execute(actionContext);

            // Then
            var actualBalance = world.GetBalance(validatorPrivateKey.Address, NCG);

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
            var validatorPrivateKey = new PrivateKey();
            var delegatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            var actionContext = new ActionContext { };
            var promotedGold = NCG * 10;
            var allocatedReward = FungibleAssetValue.Parse(NCG, $"{reward:R}");
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, promotedGold, blockHeight++);
            world = EnsureDelegatorToBeBond(
                world, delegatorPrivateKey, validatorPrivateKey, NCG * 10, blockHeight++);
            world = EnsureValidatorToBeAllocatedReward(
                world, validatorPrivateKey, allocatedReward, ref blockHeight);

            // When
            var expectedRepository = new ValidatorRepository(world, actionContext);
            var expectedDelegatee = expectedRepository.GetValidatorDelegatee(
                validatorPrivateKey.Address);
            var expectedCommission = GetCommission(
                allocatedReward, expectedDelegatee.CommissionPercentage);
            var expectedReward = allocatedReward - expectedCommission;
            var expectedValidatorBalance = expectedCommission + expectedReward.DivRem(2).Quotient;
            var expectedDelegatorBalance = expectedReward.DivRem(2).Quotient;
            var expectedRemainReward = allocatedReward;
            expectedRemainReward -= expectedValidatorBalance;
            expectedRemainReward -= expectedDelegatorBalance;

            var lastCommit = CreateLastCommit(validatorPrivateKey, blockHeight - 1);
            actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight++,
                Signer = validatorPrivateKey.Address,
                LastCommit = lastCommit,
            };
            world = new ClaimRewardValidator(validatorPrivateKey.Address).Execute(actionContext);
            actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight++,
                Signer = delegatorPrivateKey.Address,
                LastCommit = lastCommit,
            };
            world = new ClaimRewardValidator(validatorPrivateKey.Address).Execute(actionContext);

            // Then
            var repository = new ValidatorRepository(world, actionContext);
            var delegatee = repository.GetValidatorDelegatee(validatorPrivateKey.Address);
            var actualRemainReward = world.GetBalance(delegatee.CurrentLumpSumRewardsRecordAddress(), NCG);
            var actualValidatorBalance = world.GetBalance(validatorPrivateKey.Address, NCG);
            var actualDelegatorBalance = world.GetBalance(delegatorPrivateKey.Address, NCG);

            Assert.Equal(expectedRemainReward, actualRemainReward);
            Assert.Equal(expectedValidatorBalance, actualValidatorBalance);
            Assert.Equal(expectedDelegatorBalance, actualDelegatorBalance);
        }

        [Fact]
        public void Execute_TwoDelegators()
        {
            // Given
            var world = World;
            var validatorPrivateKey = new PrivateKey();
            var delegatorPrivateKey1 = new PrivateKey();
            var delegatorPrivateKey2 = new PrivateKey();
            var blockHeight = 1L;
            var actionContext = new ActionContext { };
            var promotedGold = NCG * 10;
            var allocatedReward = FungibleAssetValue.Parse(NCG, $"{34.27:R}");
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, promotedGold, blockHeight++);
            world = EnsureDelegatorToBeBond(
                world, delegatorPrivateKey1, validatorPrivateKey, NCG * 10, blockHeight++);
            world = EnsureDelegatorToBeBond(
                world, delegatorPrivateKey2, validatorPrivateKey, NCG * 10, blockHeight++);
            world = EnsureValidatorToBeAllocatedReward(
                world, validatorPrivateKey, allocatedReward, ref blockHeight);

            // When
            var expectedRepository = new ValidatorRepository(world, actionContext);
            var expectedDelegatee = expectedRepository.GetValidatorDelegatee(
                validatorPrivateKey.Address);
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

            var lastCommit = CreateLastCommit(validatorPrivateKey, blockHeight - 1);
            actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight++,
                Signer = validatorPrivateKey.Address,
                LastCommit = lastCommit,
            };
            world = new ClaimRewardValidator(validatorPrivateKey.Address).Execute(actionContext);
            actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight++,
                Signer = delegatorPrivateKey1.Address,
                LastCommit = lastCommit,
            };
            world = new ClaimRewardValidator(validatorPrivateKey.Address).Execute(actionContext);
            actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight++,
                Signer = delegatorPrivateKey2.Address,
                LastCommit = lastCommit,
            };
            world = new ClaimRewardValidator(validatorPrivateKey.Address).Execute(actionContext);

            // Then
            var repository = new ValidatorRepository(world, actionContext);
            var delegatee = repository.GetValidatorDelegatee(validatorPrivateKey.Address);
            var actualRemainReward = world.GetBalance(delegatee.CurrentLumpSumRewardsRecordAddress(), NCG);
            var actualValidatorBalance = world.GetBalance(validatorPrivateKey.Address, NCG);
            var actualDelegator1Balance = world.GetBalance(delegatorPrivateKey1.Address, NCG);
            var actualDelegator2Balance = world.GetBalance(delegatorPrivateKey2.Address, NCG);
            var actualCommunityBalance = world.GetBalance(Addresses.CommunityPool, NCG);

            Assert.Equal(expectedRemainReward, actualRemainReward);
            Assert.Equal(expectedCommunityBalance, actualCommunityBalance);
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
            var validatorPrivateKey = new PrivateKey();
            var delegatorPrivateKeys = GetRandomArray(length, _ => new PrivateKey());
            var delegatorNCGs = GetRandomArray(length, _ => GetRandomNCG());
            var blockHeight = 1L;
            var actionContext = new ActionContext();
            var promotedGold = GetRandomNCG();
            var allocatedReward = GetRandomNCG();
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, promotedGold, blockHeight++);
            world = EnsureDelegatorsToBeBond(
                world, delegatorPrivateKeys, validatorPrivateKey, delegatorNCGs, blockHeight++);
            world = EnsureValidatorToBeAllocatedReward(
                world, validatorPrivateKey, allocatedReward, ref blockHeight);

            // When
            var expectedRepository = new ValidatorRepository(world, actionContext);
            var expectedDelegatee = expectedRepository.GetValidatorDelegatee(
                validatorPrivateKey.Address);
            var expectedCommission = GetCommission(
                allocatedReward, expectedDelegatee.CommissionPercentage);
            var expectedReward = allocatedReward - expectedCommission;
            var expectedValidatorBalance = expectedCommission + CalculateReward(
                expectedRepository, validatorPrivateKey, validatorPrivateKey, expectedReward);
            var expectedDelegatorBalances = CalculateRewards(
                expectedRepository, validatorPrivateKey, delegatorPrivateKeys, expectedReward);
            var expectedCommunityBalance = allocatedReward;
            expectedCommunityBalance -= expectedValidatorBalance;
            for (var i = 0; i < length; i++)
            {
                expectedCommunityBalance -= expectedDelegatorBalances[i];
            }

            var expectedRemainReward = expectedDelegatee.RewardCurrency * 0;

            var lastCommit = CreateLastCommit(validatorPrivateKey, blockHeight - 1);
            actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight++,
                Signer = validatorPrivateKey.Address,
                LastCommit = lastCommit,
            };
            world = new ClaimRewardValidator(validatorPrivateKey.Address).Execute(actionContext);
            for (var i = 0; i < length; i++)
            {
                actionContext = new ActionContext
                {
                    PreviousState = world,
                    BlockIndex = blockHeight++,
                    Signer = delegatorPrivateKeys[i].Address,
                    LastCommit = lastCommit,
                };
                world = new ClaimRewardValidator(validatorPrivateKey.Address).Execute(actionContext);
            }

            // Then
            var repository = new ValidatorRepository(world, actionContext);
            var delegatee = repository.GetValidatorDelegatee(validatorPrivateKey.Address);
            var actualRemainReward = world.GetBalance(delegatee.CurrentLumpSumRewardsRecordAddress(), NCG);
            var actualValidatorBalance = world.GetBalance(validatorPrivateKey.Address, NCG);
            var actualCommunityBalance = world.GetBalance(Addresses.CommunityPool, NCG);
            Assert.Equal(expectedRemainReward, actualRemainReward);
            Assert.Equal(expectedValidatorBalance, actualValidatorBalance);
            Assert.Equal(expectedCommunityBalance, actualCommunityBalance);

            for (var i = 0; i < length; i++)
            {
                var actualDelegatorBalance = world.GetBalance(delegatorPrivateKeys[i].Address, NCG);
                Assert.Equal(expectedDelegatorBalances[i], actualDelegatorBalance);
            }
        }

        private static FungibleAssetValue CalculateReward(
            ValidatorRepository repository,
            PrivateKey validatorPrivateKey,
            PrivateKey delegatorPrivateKey,
            FungibleAssetValue reward)
        {
            var delegatee = repository.GetValidatorDelegatee(validatorPrivateKey.Address);
            var bond = repository.GetBond(delegatee, delegatorPrivateKey.Address);
            return CalculateReward(reward, bond.Share, delegatee.TotalShares);
        }

        private static FungibleAssetValue[] CalculateRewards(
            ValidatorRepository repository,
            PrivateKey validatorPrivateKey,
            PrivateKey[] delegatorPrivateKeys,
            FungibleAssetValue reward)
        {
            return delegatorPrivateKeys
                .Select(item => CalculateReward(repository, validatorPrivateKey, item, reward))
                .ToArray();
        }

        private static FungibleAssetValue CalculateReward(
            FungibleAssetValue reward, BigInteger share, BigInteger totalShares)
        {
            return (reward * share).DivRem(totalShares).Quotient;
        }
    }
}
