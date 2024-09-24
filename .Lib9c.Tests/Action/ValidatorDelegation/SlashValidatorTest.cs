#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Numerics;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Blocks;
    using Libplanet.Types.Consensus;
    using Libplanet.Types.Evidence;
    using Nekoyume.Action.ValidatorDelegation;
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
            var validatorPrivateKey = new PrivateKey();
            var validatorGold = NCG * 10;
            var deletatorPrivateKeys = GetRandomArray(length, _ => new PrivateKey());
            var delegatorNCGs = GetRandomArray(
                length, i => NCG * Random.Shared.Next(10, 100));
            var blockHeight = 1L;
            var actionContext = new ActionContext { };
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, NCG * 10, blockHeight++);
            world = EnsureDelegatorsToBeBond(
                world, deletatorPrivateKeys, validatorPrivateKey, delegatorNCGs, blockHeight++);

            // When
            var validatorSet = new ValidatorSet(new List<Validator>
            {
                new (validatorPrivateKey.PublicKey, new BigInteger(1000)),
            });
            var vote1 = new VoteMetadata(
                height: blockHeight - 1,
                round: 0,
                blockHash: new BlockHash(GetRandomArray(BlockHash.Size, _ => (byte)0x01)),
                timestamp: DateTimeOffset.UtcNow,
                validatorPublicKey: validatorPrivateKey.PublicKey,
                validatorPower: BigInteger.One,
                flag: VoteFlag.PreCommit).Sign(validatorPrivateKey);
            var vote2 = new VoteMetadata(
                height: blockHeight - 1,
                round: 0,
                blockHash: new BlockHash(GetRandomArray(BlockHash.Size, _ => (byte)0x02)),
                timestamp: DateTimeOffset.UtcNow,
                validatorPublicKey: validatorPrivateKey.PublicKey,
                validatorPower: BigInteger.One,
                flag: VoteFlag.PreCommit).Sign(validatorPrivateKey);
            var evidence = new DuplicateVoteEvidence(
                vote1,
                vote2,
                validatorSet,
                vote1.Timestamp);
            var lastCommit = new BlockCommit(
                height: blockHeight - 1,
                round: 0,
                blockHash: EmptyBlockHash,
                ImmutableArray.Create(vote1));
            var slashValidator = new SlashValidator();
            actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight++,
                Evidence = new List<EvidenceBase> { evidence },
                LastCommit = lastCommit,
            };
            world = slashValidator.Execute(actionContext);

            // Then
            var balance = world.GetBalance(validatorPrivateKey.Address, NCG);
            var repository = new ValidatorRepository(world, actionContext);
            var delegatee = repository.GetValidatorDelegatee(validatorPrivateKey.Address);

            Assert.True(delegatee.Jailed);
            Assert.Equal(long.MaxValue, delegatee.JailedUntil);
            Assert.True(delegatee.Tombstoned);
        }

        [Fact]
        public void Jail_By_Abstain()
        {
            // Given
            var world = World;
            var validatorPrivateKey = new PrivateKey();
            var actionContext = new ActionContext { };
            var blockHeight = 1L;
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, NCG * 10, blockHeight++);

            // When
            for (var i = 0L; i <= AbstainHistory.MaxAbstainAllowance; i++)
            {
                var vote = CreateNullVote(validatorPrivateKey, blockHeight - 1);
                var lastCommit = new BlockCommit(
                    height: blockHeight - 1,
                    round: 0,
                    blockHash: vote.BlockHash,
                    ImmutableArray.Create(vote));
                world = ExecuteSlashValidator(
                    world, validatorPrivateKey.PublicKey, lastCommit, blockHeight++);
            }

            // Then
            var repository = new ValidatorRepository(world, actionContext);
            var delegatee = repository.GetValidatorDelegatee(validatorPrivateKey.Address);
            Assert.True(delegatee.Jailed);
            Assert.False(delegatee.Tombstoned);
        }

        [Fact]
        public void Jail_JailedDelegatee_Nothing_Happens_Test()
        {
            // Given
            var world = World;
            var validatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            var actionContext = new ActionContext();
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, NCG * 10, blockHeight++);
            world = EnsureValidatorToBeTombstoned(world, validatorPrivateKey, blockHeight++);

            // When
            var expectedRepository = new ValidatorRepository(world, actionContext);
            var expectedDelegatee = expectedRepository.GetValidatorDelegatee(
                validatorPrivateKey.Address);
            var expectedJailed = expectedDelegatee.Jailed;
            var evidence = CreateDuplicateVoteEvidence(validatorPrivateKey, blockHeight - 1);
            var lastCommit = new BlockCommit(
                height: blockHeight - 1,
                round: 0,
                blockHash: EmptyBlockHash,
                ImmutableArray.Create(evidence.VoteRef));
            var slashValidator = new SlashValidator();
            actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight + SlashValidator.AbstainJailTime,
                Signer = validatorPrivateKey.Address,
                Evidence = new List<EvidenceBase> { evidence },
                LastCommit = lastCommit,
            };
            world = slashValidator.Execute(actionContext);

            // Then
            var actualRepository = new ValidatorRepository(world, actionContext);
            var actualDelegatee = actualRepository.GetValidatorDelegatee(
                validatorPrivateKey.Address);
            var actualJailed = actualDelegatee.Jailed;

            Assert.Equal(expectedJailed, actualJailed);
        }
    }
}
