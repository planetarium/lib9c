namespace Lib9c.Tests.Delegation.Migration
{
    using System.Collections.Immutable;
    using System.Linq;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Delegation;
    using Xunit;

    public class MigrateLegacyStateTest
    {
        [Fact]
        public void ParseLegacyDelegateeMetadata()
        {
            var address = new PrivateKey().Address;
            var accountAddress = new PrivateKey().Address;
            var delegationCurrency = Currency.Uncapped("del", 5, null);
            var rewardCurrencies = new Currency[] { Currency.Uncapped("rew", 5, null), };
            var delegationPoolAddress = new PrivateKey().Address;
            var rewardPoolAddress = new PrivateKey().Address;
            var rewardRemainderPoolAddress = new PrivateKey().Address;
            var slashedPoolAddress = new PrivateKey().Address;
            var unbondingPeriod = 1L;
            var maxUnbondLockInEntries = 2;
            var maxRebondGraceEntries = 3;

            var legacyDelegateeMetadataBencoded = new LegacyDelegateeMetadata(
                address,
                accountAddress,
                delegationCurrency,
                rewardCurrencies,
                delegationPoolAddress,
                rewardPoolAddress,
                rewardRemainderPoolAddress,
                slashedPoolAddress,
                unbondingPeriod,
                maxUnbondLockInEntries,
                maxRebondGraceEntries).Bencoded;

            var delegateeMetadata = new DelegateeMetadata(address, accountAddress, legacyDelegateeMetadataBencoded);

            Assert.Equal(address, delegateeMetadata.DelegateeAddress);
            Assert.Equal(accountAddress, delegateeMetadata.DelegateeAccountAddress);
            Assert.Equal(delegationCurrency, delegateeMetadata.DelegationCurrency);
            Assert.Equal(rewardCurrencies, delegateeMetadata.RewardCurrencies);
            Assert.Equal(delegationPoolAddress, delegateeMetadata.DelegationPoolAddress);
            Assert.Equal(rewardPoolAddress, delegateeMetadata.RewardPoolAddress);
            Assert.Equal(rewardRemainderPoolAddress, delegateeMetadata.RewardRemainderPoolAddress);
            Assert.Equal(slashedPoolAddress, delegateeMetadata.SlashedPoolAddress);
            Assert.Equal(unbondingPeriod, delegateeMetadata.UnbondingPeriod);
            Assert.Equal(maxUnbondLockInEntries, delegateeMetadata.MaxUnbondLockInEntries);
            Assert.Equal(maxRebondGraceEntries, delegateeMetadata.MaxRebondGraceEntries);
        }

        [Fact]
        public void ParseLegacyLumpSumRewardsRecord()
        {
            var address = new PrivateKey().Address;
            var startHeight = 1L;
            var totalShares = 2;
            var delegators = ImmutableSortedSet.Create<Address>(new PrivateKey().Address);
            var currencies = new Currency[] { Currency.Uncapped("cur", 5, null), };
            var lastStartHeight = 3L;

            var legacyLumpSumRewardsRecordBencoded = new LegacyLumpSumRewardsRecord(
                address,
                startHeight,
                totalShares,
                delegators,
                currencies,
                lastStartHeight).Bencoded;

            var lumpSumRewardsRecord = new LumpSumRewardsRecord(address, legacyLumpSumRewardsRecordBencoded);

            Assert.Equal(address, lumpSumRewardsRecord.Address);
            Assert.Equal(startHeight, lumpSumRewardsRecord.StartHeight);
            Assert.Equal(totalShares, lumpSumRewardsRecord.TotalShares);
            Assert.Equal(currencies, lumpSumRewardsRecord.LumpSumRewards.Select(c => c.Key));
        }
    }
}
