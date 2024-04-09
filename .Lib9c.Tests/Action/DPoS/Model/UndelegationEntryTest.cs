namespace Lib9c.Tests.Action.DPoS.Model
{
    using Nekoyume.Action.DPoS.Exception;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Xunit;

    public class UndelegationEntryTest : PoSTest
    {
        private readonly UndelegationEntry _undelegationEntry;

        public UndelegationEntryTest()
        {
            _undelegationEntry = new UndelegationEntry(
                CreateAddress(), Asset.ConsensusFromGovernance(1), 1, 1);
        }

        [Fact]
        public void InvalidUnbondingConsensusToken()
        {
            Assert.Throws<InvalidCurrencyException>(
                () => _undelegationEntry.UnbondingConsensusToken = Asset.GovernanceToken * 1);
            Assert.Throws<InvalidCurrencyException>(
                () => _undelegationEntry.UnbondingConsensusToken = ShareFromGovernance(1));
        }

        [Fact]
        public void MarshallingTest()
        {
            UndelegationEntry newUndelegationEntry
                = new UndelegationEntry(_undelegationEntry.Serialize());
            Assert.Equal(_undelegationEntry, newUndelegationEntry);
        }
    }
}
