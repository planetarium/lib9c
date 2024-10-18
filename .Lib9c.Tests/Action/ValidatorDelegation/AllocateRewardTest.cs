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

        PrivateKey GetProposerKey(List<Validator> validators)
        {
            return ValidatorsInfos
                .Where(item => item.VoteFlag == VoteFlag.PreCommit)
                .Where(item => validators.Any(v => v.PublicKey.Equals(item.Key.PublicKey)))
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
            TotalReward = RewardCurrency * 1000,
            ValidatorsInfos = CreateArray(4, i => new ValidatorInfo
            {
                Key = new PrivateKey(),
                Cash = DelegationCurrency * 10,
                Balance = DelegationCurrency * 100,
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
            TotalReward = FungibleAssetValue.Parse(RewardCurrency, $"{totalReward:R}"),
            ValidatorsInfos = CreateArray(validatorCount, i => new ValidatorInfo
            {
                Key = new PrivateKey(),
                Cash = DelegationCurrency * 10,
                Balance = DelegationCurrency * 100,
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
            TotalReward = RewardCurrency * 0,
            ValidatorsInfos = CreateArray(4, i => new ValidatorInfo
            {
                Key = new PrivateKey(),
                Cash = DelegationCurrency * 10,
                Balance = DelegationCurrency * 100,
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
        var height = 1L;
        world = EnsureToMintAssets(world, validatorKeys, validatorBalances, height++);
        world = EnsurePromotedValidators(world, validatorKeys, validatorCashes, height++);
        world = world.MintAsset(actionContext, Addresses.RewardPool, totalReward);
        var repository = new ValidatorRepository(world, actionContext);
        var bondedSet = repository.GetValidatorList().GetBonded();
        var proposerKey = fixture.GetProposerKey(bondedSet);
        world = EnsureProposer(world, proposerKey, height++);

        // Calculate expected values for comparison with actual values.
        var votes = CreateVotes(validatorInfos, bondedSet, height - 1);
        var expectedProposerReward
            = CalculatePropserReward(totalReward) + CalculateBonusPropserReward(votes, totalReward);
        var expectedValidatorsReward = totalReward - expectedProposerReward;
        var expectedCommunityFund = CalculateCommunityFund(votes, expectedValidatorsReward);
        var expectedAllocatedReward = totalReward - expectedCommunityFund;

        // When
        var lastCommit = new BlockCommit(height - 1, round: 0, votes[0].BlockHash, votes);
        var allocateReward = new AllocateReward();
        actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = proposerKey.PublicKey.Address,
            LastCommit = lastCommit,
            BlockIndex = height++,
        };
        world = allocateReward.Execute(actionContext);

        // Then
        var totalPower = votes.Select(item => item.ValidatorPower)
            .OfType<BigInteger>()
            .Aggregate(BigInteger.Zero, (accum, next) => accum + next);
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualAllocatedReward = RewardCurrency * 0;
        var actualCommunityFund = world.GetBalance(Addresses.CommunityPool, RewardCurrency);
        foreach (var (vote, index) in votes.Select((v, i) => (v, i)))
        {
            if (vote.ValidatorPower is not { } validatorPower)
            {
                throw new InvalidOperationException("ValidatorPower cannot be null.");
            }

            var validatorAddress = vote.ValidatorPublicKey.Address;
            var actualDelegatee = actualRepository.GetValidatorDelegatee(validatorAddress);
            var validatorRewardAddress = actualDelegatee.CurrentLumpSumRewardsRecordAddress();
            var actualDelegationBalance = world.GetBalance(validatorAddress, DelegationCurrency);
            var actualCommission = world.GetBalance(validatorAddress, RewardCurrency);
            var actualUnclaimedReward = world.GetBalance(validatorRewardAddress, RewardCurrency);
            var isProposer = vote.ValidatorPublicKey.Equals(proposerKey.PublicKey);

            if (vote.Flag == VoteFlag.Null)
            {
                Assert.Equal(RewardCurrency * 0, actualCommission);
                Assert.Equal(RewardCurrency * 0, actualUnclaimedReward);
                Assert.False(isProposer);
                continue;
            }

            var reward = (expectedValidatorsReward * validatorPower).DivRem(totalPower).Quotient;
            var expectedCommission = CalculateCommission(reward, actualDelegatee);
            var expectedUnclaimedReward = reward - expectedCommission;
            expectedCommission = isProposer
                ? expectedCommission + expectedProposerReward
                : expectedCommission;

            Assert.Equal(expectedCommission, actualCommission);
            Assert.Equal(expectedUnclaimedReward, actualUnclaimedReward);

            actualAllocatedReward += expectedCommission + expectedUnclaimedReward;
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
            TotalReward = GetRandomFAV(RewardCurrency, _random);
            ValidatorsInfos = CreateArray(_random.Next(1, 200), i =>
            {
                var balance = GetRandomFAV(DelegationCurrency, _random);
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
                var balance = GetRandomFAV(DelegationCurrency, _random);
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
