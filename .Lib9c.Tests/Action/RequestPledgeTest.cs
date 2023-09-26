namespace Lib9c.Tests.Action
{
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class RequestPledgeTest
    {
        [Theory]
        [InlineData(RequestPledge.DefaultRefillMead)]
        [InlineData(100)]
        public void Execute(int contractedMead)
        {
            Currency mead = Currencies.Mead;
            Address patron = new PrivateKey().ToAddress();
            var context = new ActionContext();
            IWorld states = LegacyModule.MintAsset(new MockWorld(), context, patron, 2 * mead);
            var address = new PrivateKey().ToAddress();
            var action = new RequestPledge
            {
                AgentAddress = address,
                RefillMead = contractedMead,
            };

            Assert.Equal(0 * mead, LegacyModule.GetBalance(states, address, mead));
            Assert.Equal(2 * mead, LegacyModule.GetBalance(states, patron, mead));

            var nextState = action.Execute(new ActionContext
            {
                Signer = patron,
                PreviousState = states,
            }).GetAccount(ReservedAddresses.LegacyAccount);
            var contract = Assert.IsType<List>(nextState.GetState(address.GetPledgeAddress()));

            Assert.Equal(patron, contract[0].ToAddress());
            Assert.False(contract[1].ToBoolean());
            Assert.Equal(contractedMead, contract[2].ToInteger());
            Assert.Equal(1 * mead, nextState.GetBalance(address, mead));
            Assert.Equal(1 * mead, nextState.GetBalance(patron, mead));
        }

        [Fact]
        public void Execute_Throw_AlreadyContractedException()
        {
            Address patron = new PrivateKey().ToAddress();
            var address = new PrivateKey().ToAddress();
            Address contractAddress = address.GetPledgeAddress();
            IWorld states = LegacyModule.SetState(new MockWorld(), contractAddress, List.Empty);
            var action = new RequestPledge
            {
                AgentAddress = address,
                RefillMead = 1,
            };

            Assert.Throws<AlreadyContractedException>(() => action.Execute(new ActionContext
            {
                Signer = patron,
                PreviousState = states,
            }));
        }
    }
}
