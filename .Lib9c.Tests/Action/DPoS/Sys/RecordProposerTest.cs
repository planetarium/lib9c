namespace Lib9c.Tests.Action.DPoS.Sys
{
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume.Action.DPoS;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Nekoyume.Action.DPoS.Sys;
    using Xunit;

    public class RecordProposerTest : PoSTest
    {
        [Fact]
        public void Execute()
        {
            var privateKey1 = new PrivateKey();
            var privateKey2 = new PrivateKey();
            var previousProposerInfo = new ProposerInfo(1, privateKey1.Address);

            // Prepare initial state.
            IWorld initialState = new World(MockWorldState.CreateModern());
            initialState = initialState.SetDPoSState(
                ReservedAddress.ProposerInfo,
                previousProposerInfo.Bencoded);
            // Execute the action.
            initialState = new RecordProposer().Execute(
                new ActionContext
                {
                    PreviousState = initialState,
                    BlockIndex = 2,
                    Miner = privateKey2.Address,
                });

            var proposerInfo =
                new ProposerInfo(initialState.GetDPoSState(ReservedAddress.ProposerInfo));
            Assert.Equal(2, proposerInfo.BlockIndex);
            Assert.True(privateKey2.Address.Equals(proposerInfo.Proposer));
        }
    }
}
