namespace Lib9c.Tests.Action
{
    using Bencodex.Types;
    using Lib9c.Action;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Xunit;

    public class RequestPledgeTest
    {
        [Theory]
        [InlineData(RequestPledge.DefaultRefillMead)]
        [InlineData(100)]
        public void Execute(int contractedMead)
        {
            var mead = Currencies.Mead;
            var patron = new PrivateKey().Address;
            var context = new ActionContext();
            var states = new World(MockUtil.MockModernWorldState).MintAsset(context, patron, 2 * mead);
            var address = new PrivateKey().Address;
            var action = new RequestPledge
            {
                AgentAddress = address,
                RefillMead = contractedMead,
            };

            Assert.Equal(0 * mead, states.GetBalance(address, mead));
            Assert.Equal(2 * mead, states.GetBalance(patron, mead));

            var nextState = action.Execute(
                new ActionContext
                {
                    Signer = patron,
                    PreviousState = states,
                });
            var contract = Assert.IsType<List>(nextState.GetLegacyState(address.GetPledgeAddress()));

            Assert.Equal(patron, contract[0].ToAddress());
            Assert.False(contract[1].ToBoolean());
            Assert.Equal(contractedMead, contract[2].ToInteger());
            Assert.Equal(1 * mead, nextState.GetBalance(address, mead));
            Assert.Equal(1 * mead, nextState.GetBalance(patron, mead));
        }

        [Fact]
        public void Execute_Throw_AlreadyContractedException()
        {
            var patron = new PrivateKey().Address;
            var address = new PrivateKey().Address;
            var contractAddress = address.GetPledgeAddress();
            var states = new World(MockUtil.MockModernWorldState).SetLegacyState(contractAddress, List.Empty);
            var action = new RequestPledge
            {
                AgentAddress = address,
                RefillMead = 1,
            };

            Assert.Throws<AlreadyContractedException>(
                () => action.Execute(
                    new ActionContext
                    {
                        Signer = patron,
                        PreviousState = states,
                    }));
        }
    }
}
