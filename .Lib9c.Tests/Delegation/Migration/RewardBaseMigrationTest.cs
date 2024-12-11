#nullable enable
namespace Lib9c.Tests.Delegation.Migration
{
    using System.Linq;
    using Nekoyume.Delegation;
    using Nekoyume.Extensions;
    using Xunit;

    public class RewardBaseMigrationTest
    {
        private readonly DelegationFixture _fixture;

        public RewardBaseMigrationTest()
        {
            _fixture = new DelegationFixture();
        }

        public LegacyTestDelegatee LegacyDelegatee
            => new LegacyTestDelegatee(
                _fixture.TestDelegatee1.Address,
                _fixture.TestRepository);

        public LegacyTestDelegator LegacyDelegator1
            => new LegacyTestDelegator(
                _fixture.TestDelegator1.Address,
                _fixture.TestRepository);

        public LegacyTestDelegator LegacyDelegator2
            => new LegacyTestDelegator(
                _fixture.TestDelegator2.Address,
                _fixture.TestRepository);

        [Fact]
        public void Migrate()
        {
            var repo = _fixture.TestRepository;

            var delegatorInitialBalance = LegacyDelegatee.DelegationCurrency * 2000;
            repo.MintAsset(LegacyDelegator1.Address, delegatorInitialBalance);
            repo.MintAsset(LegacyDelegator2.Address, delegatorInitialBalance);

            var rewards = LegacyDelegatee.RewardCurrencies.Select(r => r * 100);
            foreach (var reward in rewards)
            {
                repo.MintAsset(LegacyDelegatee.RewardPoolAddress, reward);
            }

            LegacyDelegatee.CollectRewards(7L);

            var delegatingFAV = LegacyDelegatee.DelegationCurrency * 100;
            LegacyDelegator1.Delegate(LegacyDelegatee, delegatingFAV, 10L);

            foreach (var reward in rewards)
            {
                repo.MintAsset(LegacyDelegatee.RewardPoolAddress, reward);
            }

            LegacyDelegatee.CollectRewards(13L);

            var delegatingFAV1 = LegacyDelegatee.DelegationCurrency * 100;
            LegacyDelegator2.Delegate(LegacyDelegatee, delegatingFAV1, 15L);

            foreach (var reward in rewards)
            {
                repo.MintAsset(LegacyDelegatee.RewardPoolAddress, reward);
            }

            LegacyDelegatee.CollectRewards(17L);
            var delegatingFAV2 = LegacyDelegatee.DelegationCurrency * 200;
            LegacyDelegator2.Delegate(LegacyDelegatee, delegatingFAV2, 20L);

            foreach (var reward in rewards)
            {
                repo.MintAsset(LegacyDelegatee.RewardPoolAddress, reward);
            }

            LegacyDelegatee.CollectRewards(23L);
            var delegatingFAV3 = LegacyDelegatee.DelegationCurrency * 300;
            LegacyDelegator2.Delegate(LegacyDelegatee, delegatingFAV3, 25L);

            foreach (var reward in rewards)
            {
                repo.MintAsset(LegacyDelegatee.RewardPoolAddress, reward);
            }

            LegacyDelegatee.CollectRewards(27L);
            var delegatingFAV4 = LegacyDelegatee.DelegationCurrency * 400;
            LegacyDelegator2.Delegate(LegacyDelegatee, delegatingFAV4, 30L);

            foreach (var reward in rewards)
            {
                repo.MintAsset(LegacyDelegatee.RewardPoolAddress, reward);
            }

            LegacyDelegatee.CollectRewards(23L);

            _fixture.TestRepository.UpdateWorld(_fixture.TestRepository.World.MutateAccount(
                _fixture.TestRepository.DelegateeMetadataAccountAddress,
                a => a.SetState(LegacyDelegatee.MetadataAddress, LegacyDelegatee.MetadataBencoded)));
            _fixture.TestRepository.UpdateWorld(_fixture.TestRepository.World.MutateAccount(
                _fixture.TestRepository.DelegatorMetadataAccountAddress,
                a => a.SetState(LegacyDelegatee.Metadata.Address, LegacyDelegatee.Metadata.Bencoded)));
            _fixture.TestRepository.UpdateWorld(_fixture.TestRepository.World.MutateAccount(
                _fixture.TestRepository.DelegatorMetadataAccountAddress,
                a => a.SetState(LegacyDelegatee.Metadata.Address, LegacyDelegatee.Metadata.Bencoded)));

            var delegator1 = _fixture.TestRepository.GetDelegator(_fixture.TestDelegator1.Address);
            var delegator2 = _fixture.TestRepository.GetDelegator(_fixture.TestDelegator2.Address);
            var delegatee = _fixture.TestRepository.GetDelegatee(_fixture.TestDelegatee1.Address);

            var delegatingFAV5 = delegatee.DelegationCurrency * 500;
            delegator2.Delegate(delegatee, delegatingFAV5, 35L);

            foreach (var reward in rewards)
            {
                repo.MintAsset(delegatee.RewardPoolAddress, reward);
            }

            delegatee.CollectRewards(37);

            var delegator1RewardBeforeDelegate = repo.GetBalance(delegator1.RewardAddress, DelegationFixture.TestRewardCurrency);
            Assert.Equal(DelegationFixture.TestRewardCurrency * 0, delegator1RewardBeforeDelegate);

            delegator1.Delegate(delegatee, delegatingFAV5, 40L);

            var delegator1Reward = repo.GetBalance(delegator1.RewardAddress, DelegationFixture.TestRewardCurrency);

            var expectedReward = DelegationFixture.TestRewardCurrency * 100
                + (DelegationFixture.TestRewardCurrency * 100 * 100).DivRem(200).Quotient
                + (DelegationFixture.TestRewardCurrency * 100 * 100).DivRem(400).Quotient
                + (DelegationFixture.TestRewardCurrency * 100 * 100).DivRem(700).Quotient
                + (DelegationFixture.TestRewardCurrency * 100 * 100).DivRem(1100).Quotient
                + (DelegationFixture.TestRewardCurrency * 100 * 100).DivRem(1600).Quotient;

            Assert.Equal(expectedReward.MajorUnit, delegator1Reward.MajorUnit);
        }
    }
}
