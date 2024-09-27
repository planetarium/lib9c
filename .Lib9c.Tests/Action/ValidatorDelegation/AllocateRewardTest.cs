#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;
using Nekoyume;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.Model.State;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class AllocateRewardTest : ValidatorDelegationTestBase
{
    private interface IAllocateRewardFixture
    {
        FungibleAssetValue TotalReward { get; }

        ValidatorInfo[] ValidatorsInfos { get; }

        DelegatorInfo[] Delegatorinfos { get; }

        PrivateKey[] ValidatorKeys => ValidatorsInfos.Select(info => info.Key).ToArray();

        FungibleAssetValue[] ValidatorCashes => ValidatorsInfos.Select(info => info.Cash).ToArray();

        FungibleAssetValue[] ValidatorBalances
            => ValidatorsInfos.Select(info => info.Balance).ToArray();

        PrivateKey GetProposerKey()
        {
            return ValidatorsInfos
                .Where(item => item.VoteFlag == VoteFlag.PreCommit)
                .Take(ValidatorList.MaxBondedSetSize)
                .First()
                .Key;
        }
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
        var action = new AllocateReward();
        var plainValue = action.PlainValue;

        var deserialized = new AllocateReward();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        var fixture = new StaticFixture
        {
            TotalReward = NCG * 1000,
            ValidatorsInfos = CreateArray(4, i => new ValidatorInfo
            {
                Key = new PrivateKey(),
                Cash = NCG * 10,
                Balance = NCG * 100,
                VoteFlag = i % 2 == 0 ? VoteFlag.PreCommit : VoteFlag.Null,
            }),
            Delegatorinfos = Array.Empty<DelegatorInfo>(),
        };
        ExecuteWithFixture(fixture);
    }

    [Theory]
    [InlineData(1, 1000)]
    [InlineData(33, 33)]
    [InlineData(33, 33.33)]
    [InlineData(17, 71.57)]
    [InlineData(1, 3)]
    [InlineData(10, 2.79)]
    [InlineData(1, 0.01)]
    [InlineData(10, 0.01)]
    public void Execute_Theory(int validatorCount, double totalReward)
    {
        var fixture = new StaticFixture
        {
            TotalReward = FungibleAssetValue.Parse(NCG, $"{totalReward:R}"),
            ValidatorsInfos = CreateArray(validatorCount, i => new ValidatorInfo
            {
                Key = new PrivateKey(),
                Cash = NCG * 10,
                Balance = NCG * 100,
                VoteFlag = i % 2 == 0 ? VoteFlag.PreCommit : VoteFlag.Null,
            }),
            Delegatorinfos = Array.Empty<DelegatorInfo>(),
        };
        ExecuteWithFixture(fixture);
    }

    [Fact]
    public void Execute_WithoutReward_Throw()
    {
        var fixture = new StaticFixture
        {
            TotalReward = NCG * 0,
            ValidatorsInfos = CreateArray(4, i => new ValidatorInfo
            {
                Key = new PrivateKey(),
                Cash = NCG * 10,
                Balance = NCG * 100,
                VoteFlag = i % 2 == 0 ? VoteFlag.PreCommit : VoteFlag.Null,
            }),
            Delegatorinfos = Array.Empty<DelegatorInfo>(),
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => ExecuteWithFixture(fixture));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1181126949)]
    [InlineData(793705868)]
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

    private static ImmutableArray<Vote> CreateVotes(
        ValidatorInfo[] validatorInfos, IReadOnlyList<Validator> validatorList, long blockHeight)
    {
        var infoByPublicKey = validatorInfos.ToDictionary(k => k.Key.PublicKey, k => k);
        var voteList = new List<Vote>(validatorList.Count);
        for (var i = 0; i < validatorList.Count; i++)
        {
            var validator = validatorList[i];
            var validatorInfo = infoByPublicKey[validator.PublicKey];
            var voteFlags = validatorInfo.VoteFlag;
            var privateKey = voteFlags == VoteFlag.PreCommit ? validatorInfo.Key : null;
            var voteMetadata = new VoteMetadata(
                height: blockHeight,
                round: 0,
                blockHash: EmptyBlockHash,
                timestamp: DateTimeOffset.UtcNow,
                validatorPublicKey: validator.PublicKey,
                validatorPower: validator.Power,
                flag: voteFlags);
            var vote = voteMetadata.Sign(privateKey);
            voteList.Add(vote);
        }

        return voteList.ToImmutableArray();
    }

    private void ExecuteWithStaticVariable(int validatorCount, FungibleAssetValue totalReward)
    {
        if (validatorCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(validatorCount));
        }

        if (totalReward.Sign <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalReward));
        }

        // Given
        var length = validatorCount;
        var world = World;
        var actionContext = new ActionContext { };
        var validatorInfos = CreateArray(length, i => new ValidatorInfo
        {
            Key = new PrivateKey(),
            Cash = NCG * (i + 1),
            Balance = NCG * 1000,
            VoteFlag = i % 2 == 0 ? VoteFlag.PreCommit : VoteFlag.Null,
        });
        var validatorKeys = validatorInfos.Select(info => info.Key).ToArray();
        var validatorPromotes = validatorInfos.Select(info => info.Cash).ToArray();
        var validatorBalances = validatorInfos.Select(info => info.Balance).ToArray();
        var height = 1L;
        var proposerKey = validatorInfos
            .Where(item => item.VoteFlag == VoteFlag.PreCommit)
            .Take(ValidatorList.MaxBondedSetSize)
            .First()
            .Key;

        world = EnsureToMintAssets(world, validatorKeys, validatorBalances, height++);
        world = EnsurePromotedValidators(world, validatorKeys, validatorPromotes, height++);
        world = EnsureProposer(world, proposerKey, height++);
        world = world.MintAsset(actionContext, Addresses.RewardPool, totalReward);

        // Calculate expected values for comparison with actual values.
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedBondedSet = expectedRepository.GetValidatorList().GetBonded();
        var expectedProposer = proposerKey;
        var votes = CreateVotes(validatorInfos, expectedBondedSet, height - 1);
        var balances = votes
            .Select(vote => world.GetBalance(vote.ValidatorPublicKey.Address, NCG)).ToArray();
        var expectedProposerReward
            = CalculatePropserReward(totalReward) + CalculateBonusPropserReward(votes, totalReward);
        var expectedValidatorsReward = totalReward - expectedProposerReward;
        var expectedCommunityFund = CalculateCommunityFund(votes, expectedValidatorsReward);
        var expectedAllocatedReward = expectedValidatorsReward - expectedCommunityFund;

        // When
        var lastCommit = new BlockCommit(height - 1, round: 0, votes[0].BlockHash, votes);
        var allocateReward = new AllocateReward();
        actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = expectedProposer.PublicKey.Address,
            LastCommit = lastCommit,
            BlockIndex = height++,
        };
        world = allocateReward.Execute(actionContext);

        // Then
        var totalPower = votes.Select(item => item.ValidatorPower)
            .OfType<BigInteger>()
            .Aggregate(BigInteger.Zero, (accum, next) => accum + next);
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualAllocatedReward = NCG * 0;
        var actualCommunityFund = world.GetBalance(Addresses.CommunityPool, NCG);
        foreach (var (vote, index) in votes.Select((v, i) => (v, i)))
        {
            if (vote.ValidatorPower is not { } validatorPower)
            {
                throw new InvalidOperationException("ValidatorPower cannot be null.");
            }

            var validatorAddress = vote.ValidatorPublicKey.Address;
            var actualDelegatee = actualRepository.GetValidatorDelegatee(validatorAddress);
            var validatorRewardAddress = actualDelegatee.CurrentLumpSumRewardsRecordAddress();
            var balance = balances[index];
            var actualBalance = world.GetBalance(validatorAddress, NCG);
            var actualReward = world.GetBalance(validatorRewardAddress, NCG);
            var isProposer = vote.ValidatorPublicKey.Equals(expectedProposer.PublicKey);

            if (vote.Flag == VoteFlag.Null)
            {
                Assert.Equal(balance, actualBalance);
                Assert.Equal(NCG * 0, actualReward);
                Assert.False(isProposer);
                continue;
            }

            var reward = (expectedValidatorsReward * validatorPower).DivRem(totalPower).Quotient;
            var expectedCommission = CalculateCommission(reward, actualDelegatee);
            var expectedBalance = isProposer
                ? expectedCommission + balance + expectedProposerReward
                : expectedCommission + balance;
            var expectedReward = reward - expectedCommission;

            Assert.Equal(expectedBalance, actualBalance);
            Assert.Equal(expectedReward, actualReward);

            actualAllocatedReward += expectedCommission + expectedReward;
        }

        Assert.Equal(expectedAllocatedReward, actualAllocatedReward);
        Assert.Equal(expectedCommunityFund, actualCommunityFund);
    }

    private void ExecuteWithRandomVariable(int randomSeed)
    {
        // Given
        var random = new Random(randomSeed);
        var length = random.Next(1, 200);
        var totalReward = GetRandomNCG(random);
        var world = World;
        var actionContext = new ActionContext { };
        var voteCount = 0;
        var validatorInfos = CreateArray(length, i =>
        {
            var promote = GetRandomNCG(random);
            var flag = voteCount < 1 || random.Next() % 2 == 0 ? VoteFlag.PreCommit : VoteFlag.Null;
            voteCount += flag == VoteFlag.PreCommit ? 1 : 0;
            return new ValidatorInfo
            {
                Key = new PrivateKey(),
                Cash = promote,
                Balance = promote + GetRandomNCG(random),
                VoteFlag = flag,
            };
        });
        var validatorKeys = validatorInfos.Select(info => info.Key).ToArray();
        var validatorPromotes = validatorInfos.Select(info => info.Cash).ToArray();
        var validatorBalances = validatorInfos.Select(info => info.Balance).ToArray();
        var height = 1L;
        var proposerKey = validatorInfos
            .Where(item => item.VoteFlag == VoteFlag.PreCommit)
            .Take(ValidatorList.MaxBondedSetSize)
            .First()
            .Key;

        world = EnsureToMintAssets(world, validatorKeys, validatorBalances, height++);
        world = EnsurePromotedValidators(world, validatorKeys, validatorPromotes, height++);
        world = EnsureProposer(world, proposerKey, height++);
        world = world.MintAsset(actionContext, Addresses.RewardPool, totalReward);

        // Calculate expected values for comparison with actual values.
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedBondedSet = expectedRepository.GetValidatorList().GetBonded();
        var expectedProposer = proposerKey;
        var votes = CreateVotes(validatorInfos, expectedBondedSet, height - 1);
        var balances = votes
            .Select(vote => world.GetBalance(vote.ValidatorPublicKey.Address, NCG)).ToArray();
        var expectedProposerReward
            = CalculatePropserReward(totalReward) + CalculateBonusPropserReward(votes, totalReward);
        var expectedValidatorsReward = totalReward - expectedProposerReward;
        var expectedCommunityFund = CalculateCommunityFund(votes, expectedValidatorsReward);
        var expectedAllocatedReward = expectedValidatorsReward - expectedCommunityFund;

        // When
        var lastCommit = new BlockCommit(height - 1, round: 0, votes[0].BlockHash, votes);
        var allocateReward = new AllocateReward();
        actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = expectedProposer.PublicKey.Address,
            LastCommit = lastCommit,
            BlockIndex = height++,
        };
        world = allocateReward.Execute(actionContext);

        // Then
        var totalPower = votes.Select(item => item.ValidatorPower)
            .OfType<BigInteger>()
            .Aggregate(BigInteger.Zero, (accum, next) => accum + next);
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualAllocatedReward = NCG * 0;
        var actualCommunityFund = world.GetBalance(Addresses.CommunityPool, NCG);
        foreach (var (vote, index) in votes.Select((v, i) => (v, i)))
        {
            if (vote.ValidatorPower is not { } validatorPower)
            {
                throw new InvalidOperationException("ValidatorPower cannot be null.");
            }

            var validatorAddress = vote.ValidatorPublicKey.Address;
            var actualDelegatee = actualRepository.GetValidatorDelegatee(validatorAddress);
            var validatorRewardAddress = actualDelegatee.CurrentLumpSumRewardsRecordAddress();
            var balance = balances[index];
            var actualBalance = world.GetBalance(validatorAddress, NCG);
            var actualReward = world.GetBalance(validatorRewardAddress, NCG);
            var isProposer = vote.ValidatorPublicKey.Equals(expectedProposer.PublicKey);

            if (vote.Flag == VoteFlag.Null)
            {
                Assert.Equal(balance, actualBalance);
                Assert.Equal(NCG * 0, actualReward);
                Assert.False(isProposer);
                continue;
            }

            var reward = (expectedValidatorsReward * validatorPower).DivRem(totalPower).Quotient;
            var expectedCommission = CalculateCommission(reward, actualDelegatee);
            var expectedBalance = isProposer
                ? expectedCommission + balance + expectedProposerReward
                : expectedCommission + balance;
            var expectedReward = reward - expectedCommission;

            Assert.Equal(expectedBalance, actualBalance);
            Assert.Equal(expectedReward, actualReward);

            Assert.Equal(expectedBalance, actualBalance);
            Assert.Equal(expectedReward, actualReward);

            actualAllocatedReward += expectedCommission + expectedReward;
        }

        Assert.Equal(expectedAllocatedReward, actualAllocatedReward);
        Assert.Equal(expectedCommunityFund, actualCommunityFund);
    }

    private void ExecuteWithFixture(IAllocateRewardFixture fixture)
    {
        // Given
        // var length = fixture.ValidatorLength;
        var totalReward = fixture.TotalReward;
        var world = World;
        var actionContext = new ActionContext { };
        var voteCount = fixture.ValidatorsInfos.Where(
            item => item.VoteFlag == VoteFlag.PreCommit).Count();
        var validatorInfos = fixture.ValidatorsInfos;
        var validatorKeys = fixture.ValidatorKeys;
        var validatorCashes = fixture.ValidatorCashes;
        var validatorBalances = fixture.ValidatorBalances;
        var proposerKey = fixture.GetProposerKey();
        var height = 1L;
        world = EnsureToMintAssets(world, validatorKeys, validatorBalances, height++);
        world = EnsurePromotedValidators(world, validatorKeys, validatorCashes, height++);
        world = EnsureProposer(world, proposerKey, height++);
        world = world.MintAsset(actionContext, Addresses.RewardPool, totalReward);

        // Calculate expected values for comparison with actual values.
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedBondedSet = expectedRepository.GetValidatorList().GetBonded();
        var expectedProposer = proposerKey;
        var votes = CreateVotes(validatorInfos, expectedBondedSet, height - 1);
        var balances = votes
            .Select(vote => world.GetBalance(vote.ValidatorPublicKey.Address, NCG)).ToArray();
        var expectedProposerReward
            = CalculatePropserReward(totalReward) + CalculateBonusPropserReward(votes, totalReward);
        var expectedValidatorsReward = totalReward - expectedProposerReward;
        var expectedCommunityFund = CalculateCommunityFund(votes, expectedValidatorsReward);
        var expectedAllocatedReward = expectedValidatorsReward - expectedCommunityFund;

        // When
        var lastCommit = new BlockCommit(height - 1, round: 0, votes[0].BlockHash, votes);
        var allocateReward = new AllocateReward();
        actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = expectedProposer.PublicKey.Address,
            LastCommit = lastCommit,
            BlockIndex = height++,
        };
        world = allocateReward.Execute(actionContext);

        // Then
        var totalPower = votes.Select(item => item.ValidatorPower)
            .OfType<BigInteger>()
            .Aggregate(BigInteger.Zero, (accum, next) => accum + next);
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualAllocatedReward = NCG * 0;
        var actualCommunityFund = world.GetBalance(Addresses.CommunityPool, NCG);
        foreach (var (vote, index) in votes.Select((v, i) => (v, i)))
        {
            if (vote.ValidatorPower is not { } validatorPower)
            {
                throw new InvalidOperationException("ValidatorPower cannot be null.");
            }

            var validatorAddress = vote.ValidatorPublicKey.Address;
            var actualDelegatee = actualRepository.GetValidatorDelegatee(validatorAddress);
            var validatorRewardAddress = actualDelegatee.CurrentLumpSumRewardsRecordAddress();
            var balance = balances[index];
            var actualBalance = world.GetBalance(validatorAddress, NCG);
            var actualReward = world.GetBalance(validatorRewardAddress, NCG);
            var isProposer = vote.ValidatorPublicKey.Equals(expectedProposer.PublicKey);

            if (vote.Flag == VoteFlag.Null)
            {
                Assert.Equal(balance, actualBalance);
                Assert.Equal(NCG * 0, actualReward);
                Assert.False(isProposer);
                continue;
            }

            var reward = (expectedValidatorsReward * validatorPower).DivRem(totalPower).Quotient;
            var expectedCommission = CalculateCommission(reward, actualDelegatee);
            var expectedBalance = isProposer
                ? expectedCommission + balance + expectedProposerReward
                : expectedCommission + balance;
            var expectedReward = reward - expectedCommission;

            Assert.Equal(expectedBalance, actualBalance);
            Assert.Equal(expectedReward, actualReward);

            Assert.Equal(expectedBalance, actualBalance);
            Assert.Equal(expectedReward, actualReward);

            actualAllocatedReward += expectedCommission + expectedReward;
        }

        Assert.Equal(expectedAllocatedReward, actualAllocatedReward);
        Assert.Equal(expectedCommunityFund, actualCommunityFund);
    }

    private struct ValidatorInfo
    {
        public PrivateKey Key { get; set; }

        public FungibleAssetValue Cash { get; set; }

        public FungibleAssetValue Balance { get; set; }

        public VoteFlag VoteFlag { get; set; }
    }

    private struct DelegatorInfo
    {
        public PrivateKey Key { get; set; }

        public FungibleAssetValue Cash { get; set; }

        public FungibleAssetValue Balance { get; set; }
    }

    private struct StaticFixture : IAllocateRewardFixture
    {
        public FungibleAssetValue TotalReward { get; set; }

        public ValidatorInfo[] ValidatorsInfos { get; set; }

        public DelegatorInfo[] Delegatorinfos { get; set; }
    }

    private class RandomFixture : IAllocateRewardFixture
    {
        private readonly Random _random;

        public RandomFixture(int randomSeed)
        {
            _random = new Random(randomSeed);
            TotalReward = GetRandomNCG(_random);
            ValidatorsInfos = CreateArray(_random.Next(1, 200), i =>
            {
                var balance = GetRandomNCG(_random);
                var flag = _random.Next() % 2 == 0 ? VoteFlag.PreCommit : VoteFlag.Null;
                return new ValidatorInfo
                {
                    Key = new PrivateKey(),
                    Balance = balance,
                    Cash = GetRandomCash(_random, balance),
                    VoteFlag = flag,
                };
            });
            Delegatorinfos = CreateArray(_random.Next(1, 200), i =>
            {
                var balance = GetRandomNCG(_random);
                return new DelegatorInfo
                {
                    Key = new PrivateKey(),
                    Balance = balance,
                    Cash = GetRandomCash(_random, balance),
                };
            });
        }

        public FungibleAssetValue TotalReward { get; }

        public ValidatorInfo[] ValidatorsInfos { get; }

        public DelegatorInfo[] Delegatorinfos { get; }
    }
}
