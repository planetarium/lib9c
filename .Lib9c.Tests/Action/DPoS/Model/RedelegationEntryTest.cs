namespace Lib9c.Tests.Action.DPoS.Model
{
    using Nekoyume.Action.DPoS.Exception;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Xunit;

    public class RedelegationEntryTest : PoSTest
    {
        private readonly RedelegationEntry _redelegationEntry;

        public RedelegationEntryTest()
        {
            _redelegationEntry = new RedelegationEntry(
                CreateAddress(),
                ShareFromGovernance(1),
                Asset.ConvertTokens(GovernanceToken * 1, Asset.ConsensusToken),
                ShareFromGovernance(1),
                1,
                1);
        }

        [Fact]
        public void InvalidUnbondingConsensusToken()
        {
            Assert.Throws<InvalidCurrencyException>(
                () => _redelegationEntry.RedelegatingShare = GovernanceToken * 1);
            Assert.Throws<InvalidCurrencyException>(
                () => _redelegationEntry.RedelegatingShare = Asset.ConvertTokens(GovernanceToken * 1, Asset.ConsensusToken));
            Assert.Throws<InvalidCurrencyException>(
                () => _redelegationEntry.UnbondingConsensusToken = GovernanceToken * 1);
            Assert.Throws<InvalidCurrencyException>(
                () => _redelegationEntry.UnbondingConsensusToken = ShareFromGovernance(1));
            Assert.Throws<InvalidCurrencyException>(
                () => _redelegationEntry.IssuedShare = GovernanceToken * 1);
            Assert.Throws<InvalidCurrencyException>(
                () => _redelegationEntry.IssuedShare = Asset.ConvertTokens(GovernanceToken * 1, Asset.ConsensusToken));
        }

        [Fact]
        public void MarshallingTest()
        {
            RedelegationEntry newRedelegationEntry
                = new RedelegationEntry(_redelegationEntry.Serialize());
            Assert.Equal(_redelegationEntry, newRedelegationEntry);
        }
    }
}
