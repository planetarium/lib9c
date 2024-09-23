#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Numerics;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using Libplanet.Types.Consensus;
    using Nekoyume;
    using Nekoyume.Action.ValidatorDelegation;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.Module.ValidatorDelegation;
    using Nekoyume.ValidatorDelegation;
    using Xunit;

    public class AllocateRewardTest
    {
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
            IWorld world = new World(MockUtil.MockModernWorldState);
            var context = new ActionContext { };
            var ncg = Currency.Uncapped("NCG", 2, null);
            // TODO: Use Currencies.GuildGold when it's available.
            // var gg = Currencies.GuildGold;
            var gg = ncg;
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            var privateKeys = Enumerable.Range(0, 200).Select(_ => new PrivateKey()).ToArray();
            var favs = Enumerable.Range(0, 200).Select(i => gg * (i + 1)).ToArray();

            for (int i = 0; i < 200; i++)
            {
                var signer = privateKeys[i];
                world = world.MintAsset(context, signer.Address, gg * 1000);
                world = new PromoteValidator(signer.PublicKey, favs[i]).Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = signer.Address,
                    BlockIndex = 10L,
                });
            }

            var blockHash = new BlockHash(Enumerable.Repeat((byte)0x01, BlockHash.Size).ToArray());
            var timestamp = DateTimeOffset.UtcNow;
            var voteFlags = Enumerable.Range(0, 100).Select(i => i % 2 == 0 ? VoteFlag.PreCommit : VoteFlag.Null).ToArray();
            var repository = new ValidatorRepository(world, context);
            var bondedSet = repository.GetValidatorList().GetBonded();

            var proposer = bondedSet.First();
            repository.SetProposerInfo(new ProposerInfo(9L, proposer.OperatorAddress));
            var votes = bondedSet.Select(
                (v, i) => new VoteMetadata(
                    9L, 0, blockHash, timestamp, v.PublicKey, v.Power, i % 2 == 0 ? VoteFlag.PreCommit : VoteFlag.Null)
                .Sign(i % 2 == 0 ? privateKeys.First(k => k.PublicKey.Equals(v.PublicKey)) : null)).ToImmutableArray();

            var totalReward = ncg * 1000;
            world = repository.World.MintAsset(context, Addresses.RewardPool, totalReward);

            // TODO: Remove this after delegation currency has been changed into GuildGold.
            var initialFAVs = votes.Select(vote => world.GetBalance(vote.ValidatorPublicKey.Address, ncg)).ToArray();

            context = new ActionContext
            {
                BlockIndex = 10L,
                PreviousState = world,
                Signer = privateKeys[199].Address,
                LastCommit = new BlockCommit(9L, 0, blockHash, votes),
            };

            var action = new AllocateReward();
            world = action.Execute(context);

            BigInteger totalPower = votes.Aggregate(
                BigInteger.Zero,
                (accum, next) => accum + (BigInteger)next.ValidatorPower!);

            BigInteger preCommitPower = votes.Aggregate(
                BigInteger.Zero,
                (accum, next) => accum + (BigInteger)next.ValidatorPower! * (next.Flag == VoteFlag.PreCommit ? 1 : 0));

            var baseProposerReward
                = (totalReward * ValidatorDelegatee.BaseProposerRewardNumerator)
                .DivRem(ValidatorDelegatee.BaseProposerRewardDenominator).Quotient;
            var bonusProposerReward
                = (totalReward * preCommitPower * ValidatorDelegatee.BonusProposerRewardNumerator)
                .DivRem(totalPower * ValidatorDelegatee.BonusProposerRewardDenominator).Quotient;

            var proposerReward = baseProposerReward + bonusProposerReward;
            var remains = totalReward - proposerReward;
            repository.UpdateWorld(world);

            foreach (var (vote, index) in votes.Select((v, i) => (v, i)))
            {
                var initialFAV = initialFAVs[index];
                var validator = repository.GetValidatorDelegatee(vote.ValidatorPublicKey.Address);

                FungibleAssetValue rewardAllocated
                    = (remains * vote.ValidatorPower!.Value).DivRem(totalPower).Quotient;
                FungibleAssetValue commission
                    = (rewardAllocated * ValidatorDelegatee.CommissionNumerator)
                    .DivRem(ValidatorDelegatee.CommissionDenominator).Quotient;

                if (vote.Flag == VoteFlag.Null)
                {
                    Assert.Equal(initialFAV, world.GetBalance(vote.ValidatorPublicKey.Address, ncg));
                    Assert.Equal(ncg * 0, world.GetBalance(validator.RewardDistributorAddress, ncg));
                    continue;
                }

                if (vote.ValidatorPublicKey.Equals(proposer.PublicKey))
                {
                    Assert.Equal(
                        proposerReward + commission + initialFAV,
                        world.GetBalance(vote.ValidatorPublicKey.Address, ncg));
                }
                else
                {
                    Assert.Equal(commission + initialFAV, world.GetBalance(vote.ValidatorPublicKey.Address, ncg));
                }

                Assert.Equal(rewardAllocated - commission, world.GetBalance(validator.RewardDistributorAddress, ncg));
            }
        }
    }
}
