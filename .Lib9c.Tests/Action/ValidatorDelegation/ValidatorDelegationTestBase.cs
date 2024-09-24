#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation
{
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

        public ValidatorDelegationTestBase()
        {
            var world = new World(MockUtil.MockModernWorldState);
            var goldCurrencyState = new GoldCurrencyState(NCG);
            World = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
        }

        protected static BlockHash EmptyBlockHash { get; }
            = new BlockHash(GetRandomArray(BlockHash.Size, _ => (byte)0x01));

        protected PrivateKey AdminKey { get; } = new PrivateKey();

        protected IWorld World { get; }

        protected static T[] GetRandomArray<T>(int length, Func<int, T> creator)
            => Enumerable.Range(0, length).Select(creator).ToArray();

        protected static IWorld MintAsset(
            IWorld world,
            PrivateKey delegatorPrivateKey,
            FungibleAssetValue amount,
            long blockHeight)
        {
            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight,
            };
            return world.MintAsset(actionContext, delegatorPrivateKey.Address, amount);
        }

        protected static IWorld EnsureValidatorToBePromoted(
            IWorld world,
            PrivateKey validatorPrivateKey,
            FungibleAssetValue amount,
            long blockHeight)
        {
            var validatorPublicKey = validatorPrivateKey.PublicKey;
            var promoteValidator = new PromoteValidator(validatorPublicKey, amount);
            var actionContext = new ActionContext
            {
                PreviousState = MintAsset(
                    world, validatorPrivateKey, amount, blockHeight),
                Signer = validatorPublicKey.Address,
                BlockIndex = blockHeight,
            };
            return promoteValidator.Execute(actionContext);
        }

        protected static IWorld ExecuteSlashValidator(
            IWorld world,
            PublicKey validatorPublicKey,
            BlockCommit lastCommit,
            long blockHeight)
        {
            var slashValidator = new SlashValidator();
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = validatorPublicKey.Address,
                BlockIndex = blockHeight,
                LastCommit = lastCommit,
            };
            return slashValidator.Execute(actionContext);
        }

        protected static IWorld EnsureDelegatorToBeBond(
            IWorld world,
            PrivateKey delegatorPrivateKey,
            PrivateKey validatorPrivateKey,
            FungibleAssetValue amount,
            long blockHeight)
        {
            if (blockHeight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockHeight));
            }

            var delegatorAddress = delegatorPrivateKey.Address;
            var validatorAddress = validatorPrivateKey.Address;
            var actionContext = new ActionContext
            {
                PreviousState = MintAsset(
                    world, delegatorPrivateKey, amount, blockHeight),
                BlockIndex = blockHeight,
                Signer = delegatorAddress,
            };
            var delegatorValidator = new DelegateValidator(
                validatorAddress, amount);
            return delegatorValidator.Execute(actionContext);
        }

        protected static IWorld EnsureDelegatorsToBeBond(
            IWorld world,
            PrivateKey[] delegatorPrivateKeys,
            PrivateKey validatorPrivateKey,
            FungibleAssetValue[] amounts,
            long blockHeight)
        {
            if (delegatorPrivateKeys.Length != amounts.Length)
            {
                throw new ArgumentException(
                    "The length of delegatorPrivateKeys and amounts must be the same.");
            }

            for (var i = 0; i < delegatorPrivateKeys.Length; i++)
            {
                world = EnsureDelegatorToBeBond(
                    world, delegatorPrivateKeys[i], validatorPrivateKey, amounts[i], blockHeight);
            }

            return world;
        }

        protected static IWorld EnsureValidatorToBeJailed(
            IWorld world, PrivateKey validatorPrivateKey, ref long blockHeight)
        {
            if (blockHeight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockHeight));
            }

            var repository = new ValidatorRepository(world, new ActionContext());
            var delegatee = repository.GetValidatorDelegatee(validatorPrivateKey.Address);
            if (delegatee.Jailed)
            {
                throw new ArgumentException(
                    "The validator is already jailed.", nameof(validatorPrivateKey));
            }

            for (var i = 0L; i <= AbstainHistory.MaxAbstainAllowance; i++)
            {
                var vote = CreateNullVote(validatorPrivateKey, blockHeight - 1);
                var lastCommit = new BlockCommit(
                    height: blockHeight - 1,
                    round: 0,
                    blockHash: vote.BlockHash,
                    ImmutableArray.Create(vote));
                world = ExecuteSlashValidator(
                    world, validatorPrivateKey.PublicKey, lastCommit, blockHeight);
                blockHeight++;
                repository = new ValidatorRepository(world, new ActionContext());
                delegatee = repository.GetValidatorDelegatee(validatorPrivateKey.Address);
                if (delegatee.Jailed)
                {
                    break;
                }
            }

            return world;
        }

        protected static IWorld EnsureValidatorToBeTombstoned(
            IWorld world, PrivateKey validatorPrivateKey, long blockHeight)
        {
            if (blockHeight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockHeight));
            }

            var evidence = CreateDuplicateVoteEvidence(validatorPrivateKey, blockHeight - 1);
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
            PrivateKey validatorPrivateKey, long blockHeight)
        {
            if (blockHeight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockHeight));
            }

            var validatorSet = new ValidatorSet(new List<Validator>
            {
                new (validatorPrivateKey.PublicKey, new BigInteger(1000)),
            });
            var vote1 = new VoteMetadata(
                height: blockHeight,
                round: 0,
                blockHash: new BlockHash(GetRandomArray(BlockHash.Size, _ => (byte)0x01)),
                timestamp: DateTimeOffset.UtcNow,
                validatorPublicKey: validatorPrivateKey.PublicKey,
                validatorPower: BigInteger.One,
                flag: VoteFlag.PreCommit).Sign(validatorPrivateKey);
            var vote2 = new VoteMetadata(
                height: blockHeight,
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

            return evidence;
        }
    }
}
