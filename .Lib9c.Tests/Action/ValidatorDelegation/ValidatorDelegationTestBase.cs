#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Lib9c.Tests.Util;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;
using Libplanet.Types.Evidence;
using Nekoyume;
using Nekoyume.Action.Guild;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.Model.Guild;
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class ValidatorDelegationTestBase
{
    protected static readonly Currency GoldCurrency = Currency.Uncapped("NCG", 2, null);
    protected static readonly Currency DelegationCurrency = Currencies.GuildGold;
    protected static readonly Currency RewardCurrency = Currencies.Mead;
    protected static readonly Currency Dollar = Currency.Uncapped("dollar", 2, null);
    private static readonly int _maximumIntegerLength = 15;

    public ValidatorDelegationTestBase()
    {
        var world = new World(MockUtil.MockModernWorldState);
        var goldCurrencyState = new GoldCurrencyState(GoldCurrency);
        World = world
            .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
    }

    protected static BlockHash EmptyBlockHash { get; }
        = new BlockHash(CreateArray(BlockHash.Size, _ => (byte)0x01));

    protected PrivateKey AdminKey { get; } = new PrivateKey();

    protected IWorld World { get; }

    protected FungibleAssetValue MinimumDelegation { get; } = DelegationCurrency * 10;

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
        var address = privateKey.Address;
        var poolAddress = StakeState.DeriveAddress(address);
        return world.MintAsset(actionContext, poolAddress, amount);
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

    protected static IWorld EnsureUnbondingValidator(
        IWorld world,
        Address validatorAddress,
        BigInteger share,
        long blockHeight)
    {
        if (blockHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHeight));
        }

        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
            Signer = validatorAddress,
        };
        var undelegateValidator = new UndelegateValidator(
            validatorAddress, share);
        return undelegateValidator.Execute(actionContext);
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

    protected static IWorld EnsureMakeGuild(
        IWorld world,
        Address guildMasterAddress,
        Address validatorAddress,
        long blockHeight)
    {
        if (blockHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHeight));
        }

        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
            Signer = guildMasterAddress,
        };
        var guildAddress = AddressUtil.CreateGuildAddress();
        var makeGuild = new MakeGuild(guildAddress, validatorAddress);
        return makeGuild.Execute(actionContext);
    }

    protected static IWorld EnsureJoinGuild(
        IWorld world,
        Address guildParticipantAddress,
        Address guildMasterAddress,
        Address validatorAddress,
        long blockHeight)
    {
        if (blockHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHeight));
        }

        var repo = new GuildRepository(world, new ActionContext());
        var guildAddress = repo.GetJoinedGuild(new AgentAddress(guildMasterAddress))
            ?? throw new ArgumentException($"Guild master {guildMasterAddress} does not have guild");
        if (validatorAddress != repo.GetGuild(guildAddress).ValidatorAddress)
        {
            throw new ArgumentException(
                $"The guild of guild master does not belong to validator {validatorAddress}.");
        }

        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
            Signer = guildParticipantAddress,
        };
        var joinGuild = new JoinGuild(guildAddress);
        return joinGuild.Execute(actionContext);
    }

    protected static IWorld EnsureQuitGuild(
        IWorld world,
        Address guildParticipantAddress,
        long blockHeight)
    {
        if (blockHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHeight));
        }

        var delegatorAddress = guildParticipantAddress;
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
            Signer = delegatorAddress,
        };
        var undelegateValidator = new QuitGuild();
        return undelegateValidator.Execute(actionContext);
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

    protected static IWorld EnsureUnjailedValidator(
        IWorld world, PrivateKey validatorKey, ref long blockHeight)
    {
        if (blockHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHeight));
        }

        var repository = new ValidatorRepository(world, new ActionContext());
        var delegatee = repository.GetValidatorDelegatee(validatorKey.Address);
        if (!delegatee.Jailed)
        {
            throw new ArgumentException(
                "The validator is not jailed.", nameof(validatorKey));
        }

        if (delegatee.Tombstoned)
        {
            throw new ArgumentException(
                "The validator is tombstoned.", nameof(validatorKey));
        }

        blockHeight = Math.Max(blockHeight, delegatee.JailedUntil + 1);

        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = blockHeight,
            Signer = validatorKey.Address,
        };
        var unjailedValidator = new UnjailValidator();
        return unjailedValidator.Execute(actionContext);
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

    protected static IWorld EnsureCommissionChangedValidator(
        IWorld world,
        PrivateKey validatorKey,
        BigInteger commissionPercentage,
        ref long blockHeight)
    {
        if (blockHeight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHeight));
        }

        if (commissionPercentage < ValidatorDelegatee.MinCommissionPercentage
            || commissionPercentage > ValidatorDelegatee.MaxCommissionPercentage)
        {
            throw new ArgumentOutOfRangeException(nameof(commissionPercentage));
        }

        var cooldown = ValidatorDelegatee.CommissionPercentageUpdateCooldown;
        var repository = new ValidatorRepository(world, new ActionContext());
        var delegatee = repository.GetValidatorDelegatee(validatorKey.Address);
        var currentCommission = delegatee.CommissionPercentage;
        var increment = commissionPercentage > currentCommission ? 1 : -1;
        var preferredHeight = delegatee.CommissionPercentageLastUpdateHeight + cooldown;

        while (commissionPercentage != currentCommission)
        {
            blockHeight = Math.Min(preferredHeight, blockHeight + cooldown);
            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight,
                Signer = validatorKey.Address,
            };
            var setValidatorCommission = new SetValidatorCommission(
                validatorKey.Address, currentCommission + increment);
            world = setValidatorCommission.Execute(actionContext);
            currentCommission += increment;
            preferredHeight = blockHeight + cooldown;
        }

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

    protected static FungibleAssetValue GetRandomFAV(Currency currency) => GetRandomFAV(currency, Random.Shared);

    protected static FungibleAssetValue GetRandomFAV(Currency currency, Random random)
    {
        var decimalLength = random.Next(currency.DecimalPlaces);
        var integerLength = random.Next(1, _maximumIntegerLength);
        var decimalPart = Enumerable.Range(0, decimalLength)
            .Aggregate(string.Empty, (s, i) => s + random.Next(10));
        var integerPart = Enumerable.Range(0, integerLength)
            .Aggregate(string.Empty, (s, i) => s + (integerLength > 1 ? random.Next(10) : random.Next(1, 10)));
        var isDecimalZero = decimalLength == 0 || decimalPart.All(c => c == '0');
        var text = isDecimalZero is false ? $"{integerPart}.{decimalPart}" : integerPart;

        return FungibleAssetValue.Parse(currency, text);
    }

    protected static FungibleAssetValue GetRandomCash(Random random, FungibleAssetValue fav, int maxDivisor = 100)
    {
        Assert.True(maxDivisor > 0 && maxDivisor <= 100);
        var denominator = random.Next(maxDivisor) + 1;
        var cash = fav.DivRem(denominator, out var remainder);
        if (cash.Sign < 0 || cash > fav)
        {
            throw new InvalidOperationException("Invalid cash value.");
        }

        return cash;
    }

    protected static FungibleAssetValue GetBalance(IWorld world, Address address)
    {
        var poolAddress = StakeState.DeriveAddress(address);
        return world.GetBalance(poolAddress, DelegationCurrency);
    }
}
