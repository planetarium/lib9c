#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;
using Libplanet.Types.Evidence;
using Nekoyume.Action;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.Model.Guild;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class SlashValidatorTest : ValidatorDelegationTestBase
{
    [Fact]
    public void Serialization()
    {
        var action = new SlashValidator();
        var plainValue = action.PlainValue;

        var deserialized = new SlashValidator();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        // Given
        const int length = 10;
        var world = World;
        var validatorKey = new PrivateKey();
        var validatorGold = DelegationCurrency * 10;
        var delegatorKeys = CreateArray(length, _ => new PrivateKey());
        var delegatorGolds = CreateArray(length, i => DelegationCurrency * Random.Shared.Next(10, 100));
        var height = 1L;
        var actionContext = new ActionContext { };
        int seed = 0;
        world = EnsureToMintAsset(world, validatorKey, DelegationCurrency * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey, DelegationCurrency * 10, height++);
        world = EnsureToMintAssets(world, delegatorKeys, delegatorGolds, height++);
        world = delegatorKeys.Aggregate(world, (w, d) => EnsureMakeGuild(
            w, d.Address, validatorKey.Address, height++, seed++));

        // When
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetDelegatee(validatorKey.Address);
        var expectedValidatorShare = expectedRepository.GetBond(
            expectedDelegatee, validatorKey.Address).Share;

        var validatorSet = new ValidatorSet(new List<Validator>
        {
            new (validatorKey.PublicKey, new BigInteger(1000)),
        });
        var vote1 = new VoteMetadata(
            height: height - 1,
            round: 0,
            blockHash: new BlockHash(CreateArray(BlockHash.Size, _ => (byte)0x01)),
            timestamp: DateTimeOffset.UtcNow,
            validatorPublicKey: validatorKey.PublicKey,
            validatorPower: BigInteger.One,
            flag: VoteFlag.PreCommit).Sign(validatorKey);
        var vote2 = new VoteMetadata(
            height: height - 1,
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
        var lastCommit = new BlockCommit(
            height: height - 1,
            round: 0,
            blockHash: EmptyBlockHash,
            ImmutableArray.Create(vote1));
        var slashValidator = new SlashValidator();
        actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Evidence = new List<EvidenceBase> { evidence },
            LastCommit = lastCommit,
        };
        world = slashValidator.Execute(actionContext);

        // Then
        var balance = GetBalance(world, validatorKey.Address);
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualDelegatee = actualRepository.GetDelegatee(validatorKey.Address);
        var actualValidatorShare = actualRepository.GetBond(actualDelegatee, validatorKey.Address).Share;

        Assert.True(actualDelegatee.Jailed);
        Assert.Equal(long.MaxValue, actualDelegatee.JailedUntil);
        Assert.True(actualDelegatee.Tombstoned);

        // guild
        var guildRepository = new GuildRepository(world, actionContext);
        var guildDelegatee = guildRepository.GetDelegatee(validatorKey.Address);
        Assert.True(guildDelegatee.Jailed);
        Assert.Equal(long.MaxValue, guildDelegatee.JailedUntil);
        Assert.True(guildDelegatee.Tombstoned);
    }

    [Fact]
    public void Execute_ToNotPromotedValidator_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;

        // When
        var evidence = CreateDuplicateVoteEvidence(validatorKey, height - 1);
        var lastCommit = new BlockCommit(
            height: height - 1,
            round: 0,
            blockHash: EmptyBlockHash,
            ImmutableArray.Create(evidence.VoteRef));
        var slashValidator = new SlashValidator();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Evidence = new List<EvidenceBase> { evidence },
            LastCommit = lastCommit,
        };

        // Then
        Assert.Throws<FailedLoadStateException>(() => slashValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_ByAbstain()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var actionContext = new ActionContext { };
        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey, DelegationCurrency * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey, DelegationCurrency * 10, height++);

        // When
        for (var i = 0L; i <= AbstainHistory.MaxAbstainAllowance; i++)
        {
            var vote = CreateNullVote(validatorKey, height - 1);
            var lastCommit = new BlockCommit(
                height: height - 1,
                round: 0,
                blockHash: vote.BlockHash,
                ImmutableArray.Create(vote));
            var slashValidator = new SlashValidator();
            actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = validatorKey.Address,
                BlockIndex = height++,
                LastCommit = lastCommit,
            };
            world = slashValidator.Execute(actionContext);
        }

        // Then
        var repository = new ValidatorRepository(world, actionContext);
        var delegatee = repository.GetDelegatee(validatorKey.Address);
        Assert.True(delegatee.Jailed);
        Assert.False(delegatee.Tombstoned);

        // guild
        var guildRepository = new GuildRepository(world, actionContext);
        var guildDelegatee = guildRepository.GetDelegatee(validatorKey.Address);
        Assert.True(guildDelegatee.Jailed);
        Assert.False(guildDelegatee.Tombstoned);
    }

    [Fact]
    public void Execute_ToJailedValidator_ThenNothingHappens()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        var actionContext = new ActionContext();
        world = EnsureToMintAsset(world, validatorKey, DelegationCurrency * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey, DelegationCurrency * 10, height++);
        world = EnsureTombstonedValidator(world, validatorKey, height++);

        // When
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetDelegatee(validatorKey.Address);
        var expectedJailed = expectedDelegatee.Jailed;
        var evidence = CreateDuplicateVoteEvidence(validatorKey, height - 1);
        var lastCommit = new BlockCommit(
            height: height - 1,
            round: 0,
            blockHash: EmptyBlockHash,
            ImmutableArray.Create(evidence.VoteRef));
        var slashValidator = new SlashValidator();
        actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height + SlashValidator.AbstainJailTime,
            Signer = validatorKey.Address,
            Evidence = new List<EvidenceBase> { evidence },
            LastCommit = lastCommit,
        };
        world = slashValidator.Execute(actionContext);

        // Then
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualDelegatee = actualRepository.GetDelegatee(validatorKey.Address);

        Assert.Equal(expectedJailed, actualDelegatee.Jailed);

        // guild
        var guildRepository = new GuildRepository(world, actionContext);
        var guildDelegatee = guildRepository.GetDelegatee(validatorKey.Address);
        Assert.Equal(expectedJailed, guildDelegatee.Jailed);
    }

    [Fact]
    public void Execute_ByAbstain_ToJailedValidator_ThenNothingHappens()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        var actionContext = new ActionContext();
        world = EnsureToMintAsset(world, validatorKey, DelegationCurrency * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey, DelegationCurrency * 10, height++);
        world = EnsureJailedValidator(world, validatorKey, ref height);

        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetDelegatee(validatorKey.Address);
        var expectedTotalDelegated = expectedDelegatee.TotalDelegated;

        // When
        for (var i = 0L; i <= AbstainHistory.MaxAbstainAllowance; i++)
        {
            var vote = CreateNullVote(validatorKey, height - 1);
            var lastCommit = new BlockCommit(
                height: height - 1,
                round: 0,
                blockHash: vote.BlockHash,
                ImmutableArray.Create(vote));
            var slashValidator = new SlashValidator();
            actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = validatorKey.Address,
                BlockIndex = height++,
                LastCommit = lastCommit,
            };
            world = slashValidator.Execute(actionContext);
        }

        // Then
        var actualRepisitory = new ValidatorRepository(world, actionContext);
        var actualDelegatee = actualRepisitory.GetDelegatee(validatorKey.Address);

        Assert.True(actualDelegatee.Jailed);
        Assert.False(actualDelegatee.Tombstoned);
        Assert.Equal(expectedTotalDelegated, actualDelegatee.TotalDelegated);

        // guild
        var guildRepository = new GuildRepository(world, actionContext);
        var guildDelegatee = guildRepository.GetDelegatee(validatorKey.Address);
        Assert.True(guildDelegatee.Jailed);
        Assert.False(guildDelegatee.Tombstoned);
        Assert.Equal(expectedTotalDelegated, guildDelegatee.TotalDelegated);
    }
}
