#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;
using Libplanet.Types.Evidence;
using Nekoyume;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.ValidatorDelegation;

public class ValidatorDelegationTestBase
{
    protected static readonly Currency NCG = Currency.Uncapped("NCG", 2, null);
    protected static readonly Currency Dollar = Currency.Uncapped("dollar", 2, null);

    public ValidatorDelegationTestBase()
    {
        var world = new World(MockUtil.MockModernWorldState);
        var goldCurrencyState = new GoldCurrencyState(NCG);
        World = world
            .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
    }

    protected static BlockHash EmptyBlockHash { get; }
        = new BlockHash(CreateArray(BlockHash.Size, _ => (byte)0x01));

    protected PrivateKey AdminKey { get; } = new PrivateKey();

    protected IWorld World { get; }

    protected static T[] CreateArray<T>(int length, Func<int, T> creator)
        => Enumerable.Range(0, length).Select(creator).ToArray();

    protected static IWorld EnsureToMintAsset(
        IWorld world, PrivateKey privateKey, FungibleAssetValue amount, long blockHeight)
    {
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
        };
        return world.MintAsset(actionContext, privateKey.Address, amount);
    }

    protected static IWorld EnsureToMintAssets(
        IWorld world, PrivateKey[] privateKeys, FungibleAssetValue[] amounts, long blockHeight)
    {
        if (privateKeys.Length != amounts.Length)
        {
            throw new ArgumentException(
                "The length of privateKeys and amounts must be the same.");
        }

        for (var i = 0; i < privateKeys.Length; i++)
        {
            world = EnsureToMintAsset(world, privateKeys[i], amounts[i], blockHeight);
        }

        return world;
    }

    protected static IWorld EnsureProposer(
        IWorld world, PrivateKey validatorKey, long blockHeight)
    {
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
            Signer = validatorKey.Address,
            Miner = validatorKey.Address,
        };
        return new RecordProposer().Execute(actionContext);
    }

    protected static IWorld EnsurePromotedValidators(
        IWorld world,
        PrivateKey[] validatorKeys,
        FungibleAssetValue[] amounts,
        long blockHeight)
    {
        if (validatorKeys.Length != amounts.Length)
        {
            throw new ArgumentException(
                "The length of validatorPrivateKeys and amounts must be the same.");
        }

        for (var i = 0; i < validatorKeys.Length; i++)
        {
            world = EnsurePromotedValidator(
                world, validatorKeys[i], amounts[i], blockHeight);
        }

        return world;
    }

    protected static IWorld EnsurePromotedValidator(
        IWorld world,
        PrivateKey validatorKey,
        FungibleAssetValue amount,
        long blockHeight)
    {
        var validatorPublicKey = validatorKey.PublicKey;
        var promoteValidator = new PromoteValidator(validatorPublicKey, amount);

        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorPublicKey.Address,
            BlockIndex = blockHeight,
        };
        return promoteValidator.Execute(actionContext);
    }

    protected static IWorld ExecuteSlashValidator(
        IWorld world, PublicKey validatorKey, BlockCommit lastCommit, long blockHeight)
    {
        var slashValidator = new SlashValidator();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
            BlockIndex = blockHeight,
            LastCommit = lastCommit,
        };
        return slashValidator.Execute(actionContext);
    }

    protected static IWorld EnsureBondedDelegator(
        IWorld world,
        PrivateKey delegatorKey,
        PrivateKey validatorKey,
        FungibleAssetValue amount,
        long blockHeight)
    {
        if (blockHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHeight));
        }

        var delegatorAddress = delegatorKey.Address;
        var validatorAddress = validatorKey.Address;
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
            Signer = delegatorAddress,
        };
        var delegatorValidator = new DelegateValidator(
            validatorAddress, amount);
        return delegatorValidator.Execute(actionContext);
    }

    protected static IWorld EnsureBondedDelegators(
        IWorld world,
        PrivateKey[] delegatorKeys,
        PrivateKey validatorKey,
        FungibleAssetValue[] amounts,
        long blockHeight)
    {
        if (delegatorKeys.Length != amounts.Length)
        {
            throw new ArgumentException(
                "The length of delegatorPrivateKeys and amounts must be the same.");
        }

        for (var i = 0; i < delegatorKeys.Length; i++)
        {
            world = EnsureBondedDelegator(
                world, delegatorKeys[i], validatorKey, amounts[i], blockHeight);
        }

        return world;
    }

    protected static IWorld EnsureJailedValidator(
        IWorld world, PrivateKey validatorKey, ref long blockHeight)
    {
        if (blockHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHeight));
        }

        var repository = new ValidatorRepository(world, new ActionContext());
        var delegatee = repository.GetValidatorDelegatee(validatorKey.Address);
        if (delegatee.Jailed)
        {
            throw new ArgumentException(
                "The validator is already jailed.", nameof(validatorKey));
        }

        for (var i = 0L; i <= AbstainHistory.MaxAbstainAllowance; i++)
        {
            var vote = CreateNullVote(validatorKey, blockHeight - 1);
            var lastCommit = new BlockCommit(
                height: blockHeight - 1,
                round: 0,
                blockHash: vote.BlockHash,
                ImmutableArray.Create(vote));
            world = ExecuteSlashValidator(
                world, validatorKey.PublicKey, lastCommit, blockHeight);
            blockHeight++;
            repository = new ValidatorRepository(world, new ActionContext());
            delegatee = repository.GetValidatorDelegatee(validatorKey.Address);
            if (delegatee.Jailed)
            {
                break;
            }
        }

        return world;
    }

    protected static IWorld EnsureTombstonedValidator(
        IWorld world, PrivateKey validatorKey, long blockHeight)
    {
        if (blockHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHeight));
        }

        var evidence = CreateDuplicateVoteEvidence(validatorKey, blockHeight - 1);
        var lastCommit = new BlockCommit(
            height: blockHeight - 1,
            round: 0,
            blockHash: EmptyBlockHash,
            ImmutableArray.Create(evidence.VoteRef));

        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
            Evidence = new List<EvidenceBase> { evidence },
            LastCommit = lastCommit,
        };
        var slashValidator = new SlashValidator();

        return slashValidator.Execute(actionContext);
    }

    protected static IWorld EnsureRewardAllocatedValidator(
        IWorld world, PrivateKey validatorKey, FungibleAssetValue reward, ref long blockHeight)
    {
        if (blockHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHeight));
        }

        var actionContext1 = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight++,
            Signer = validatorKey.Address,
            Miner = validatorKey.Address,
        };
        world = new RecordProposer().Execute(actionContext1);

        var lastCommit2 = CreateLastCommit(validatorKey, blockHeight - 1);
        var actionContext2 = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
            Signer = validatorKey.Address,
            LastCommit = lastCommit2,
        };
        world = world.MintAsset(actionContext2, GoldCurrencyState.Address, reward);
        world = world.TransferAsset(
            actionContext2, GoldCurrencyState.Address, Addresses.RewardPool, reward);

        var lastCommit3 = CreateLastCommit(validatorKey, blockHeight - 1);
        var actionContext3 = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
            Signer = validatorKey.Address,
            LastCommit = lastCommit3,
        };
        world = new AllocateReward().Execute(actionContext3);

        return world;
    }

    protected static Vote CreateNullVote(
        PrivateKey privateKey, long blockHeight)
    {
        if (blockHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHeight));
        }

        var power = new BigInteger(100);
        var validator = new Validator(privateKey.PublicKey, power);
        var blockHash = EmptyBlockHash;
        var timestamp = DateTimeOffset.UtcNow;
        var voteMetadata = new VoteMetadata(
            height: blockHeight,
            round: 0,
            blockHash: blockHash,
            timestamp: timestamp,
            validatorPublicKey: validator.PublicKey,
            validatorPower: power,
            flag: VoteFlag.Null);
        return voteMetadata.Sign(null);
    }

    protected static Vote CreateVote(
        PrivateKey privateKey, long blockHeight)
    {
        if (blockHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHeight));
        }

        var power = new BigInteger(100);
        var validator = new Validator(privateKey.PublicKey, power);
        var blockHash = EmptyBlockHash;
        var timestamp = DateTimeOffset.UtcNow;
        var voteMetadata = new VoteMetadata(
            height: blockHeight,
            round: 0,
            blockHash: blockHash,
            timestamp: timestamp,
            validatorPublicKey: validator.PublicKey,
            validatorPower: power,
            flag: VoteFlag.PreCommit);
        return voteMetadata.Sign(privateKey);
    }

    protected static BlockCommit CreateLastCommit(
        PrivateKey privateKey, long blockHeight)
    {
        if (blockHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHeight));
        }

        var vote = CreateVote(privateKey, blockHeight);
        return new BlockCommit(
            height: blockHeight,
            round: 0,
            blockHash: vote.BlockHash,
            ImmutableArray.Create(vote));
    }

    protected static DuplicateVoteEvidence CreateDuplicateVoteEvidence(
        PrivateKey validatorKey, long blockHeight)
    {
        if (blockHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHeight));
        }

        var validatorSet = new ValidatorSet(new List<Validator>
            {
                new (validatorKey.PublicKey, new BigInteger(1000)),
            });
        var vote1 = new VoteMetadata(
            height: blockHeight,
            round: 0,
            blockHash: new BlockHash(CreateArray(BlockHash.Size, _ => (byte)0x01)),
            timestamp: DateTimeOffset.UtcNow,
            validatorPublicKey: validatorKey.PublicKey,
            validatorPower: BigInteger.One,
            flag: VoteFlag.PreCommit).Sign(validatorKey);
        var vote2 = new VoteMetadata(
            height: blockHeight,
            round: 0,
            blockHash: new BlockHash(CreateArray(BlockHash.Size, _ => (byte)0x02)),
            timestamp: DateTimeOffset.UtcNow,
            validatorPublicKey: validatorKey.PublicKey,
            validatorPower: BigInteger.One,
            flag: VoteFlag.PreCommit).Sign(validatorKey);
        var evidence = new DuplicateVoteEvidence(
            vote1,
            vote2,
            validatorSet,
            vote1.Timestamp);

        return evidence;
    }

    protected static FungibleAssetValue CalculateCommission(
        FungibleAssetValue gold, ValidatorDelegatee delegatee)
        => CalculateCommission(gold, delegatee.CommissionPercentage);

    protected static FungibleAssetValue CalculateCommission(
        FungibleAssetValue gold, BigInteger percentage)
        => (gold * percentage).DivRem(100).Quotient;

    protected static FungibleAssetValue CalculatePropserReward(FungibleAssetValue reward)
        => (reward * ValidatorDelegatee.BaseProposerRewardPercentage).DivRem(100).Quotient;

    protected static FungibleAssetValue CalculateBonusPropserReward(
        BigInteger preCommitPower, BigInteger totalPower, FungibleAssetValue reward)
        => (reward * preCommitPower * ValidatorDelegatee.BonusProposerRewardPercentage)
            .DivRem(totalPower * 100).Quotient;

    protected static FungibleAssetValue CalculateBonusPropserReward(
        ImmutableArray<Vote> votes, FungibleAssetValue reward)
    {
        var totalPower = votes.Select(item => item.ValidatorPower)
            .OfType<BigInteger>()
            .Aggregate(BigInteger.Zero, (accum, next) => accum + next);

        var preCommitPower = votes.Where(item => item.Flag == VoteFlag.PreCommit)
            .Select(item => item.ValidatorPower)
            .OfType<BigInteger>()
            .Aggregate(BigInteger.Zero, (accum, next) => accum + next);

        return CalculateBonusPropserReward(preCommitPower, totalPower, reward);
    }

    protected static FungibleAssetValue CalculateClaim(BigInteger share, BigInteger totalShare, FungibleAssetValue totalClaim)
        => (totalClaim * share).DivRem(totalShare).Quotient;

    protected static FungibleAssetValue CalculateCommunityFund(ImmutableArray<Vote> votes, FungibleAssetValue reward)
    {
        var totalPower = votes.Select(item => item.ValidatorPower)
            .OfType<BigInteger>()
            .Aggregate(BigInteger.Zero, (accum, next) => accum + next);

        var powers = votes.Where(item => item.Flag == VoteFlag.PreCommit)
            .Select(item => item.ValidatorPower)
            .OfType<BigInteger>();

        var communityFund = reward;
        foreach (var power in powers)
        {
            var distribution = (reward * power).DivRem(totalPower).Quotient;
            System.Diagnostics.Trace.WriteLine($"expected validator distribution: {reward} * {power} / {totalPower} = {distribution}");
            communityFund -= distribution;
        }

        return communityFund;
    }

    protected static FungibleAssetValue GetRandomNCG() => GetRandomNCG(Random.Shared, 1, 100000);

    protected static FungibleAssetValue GetRandomNCG(Random random)
        => GetRandomNCG(random, 0.01m, 1000.0m);

    protected static FungibleAssetValue GetRandomNCG(Random random, decimal min, decimal max)
    {
        var minLong = (int)(min * 100);
        var maxLong = (int)(max * 100);
        var value = Math.Round(random.Next(minLong, maxLong) / 100.0, 2);
        return FungibleAssetValue.Parse(NCG, $"{value:R}");
    }

    protected static FungibleAssetValue GetRandomCash(Random random, FungibleAssetValue fav)
    {
        var num = random.Next(1, 10000);
        var cash = (fav * num).DivRem(10000).Quotient;
        if (cash.Sign < 0 || cash > fav)
        {
            throw new InvalidOperationException("Invalid cash value.");
        }

        return cash;
    }
}
