namespace Lib9c.Tests.Action
{
    using System;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class EndPledgeTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public void Execute(int balance)
        {
            var patron = new PrivateKey().Address;
            var agent = new PrivateKey().Address;
            var context = new ActionContext();
            var states = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(agent.GetPledgeAddress(), List.Empty.Add(patron.Serialize()).Add(true.Serialize()));
            var mead = Currencies.Mead;
            if (balance > 0)
            {
                states = states.MintAsset(context, agent, mead * balance);
            }

            var action = new EndPledge
            {
                AgentAddress = agent,
            };
            var nextState = action.Execute(
                new ActionContext
                {
                    Signer = patron,
                    PreviousState = states,
                });
            Assert.Null(nextState.GetLegacyState(agent.GetPledgeAddress()));
            Assert.Equal(mead * 0, nextState.GetBalance(agent, mead));
            if (balance > 0)
            {
                Assert.Equal(mead * balance, nextState.GetBalance(patron, mead));
            }
        }

        [Theory]
        [InlineData(true, false, typeof(InvalidAddressException))]
        [InlineData(false, true, typeof(FailedLoadStateException))]
        public void Execute_Throw_Exception(bool invalidSigner, bool invalidAgent, Type exc)
        {
            var patron = new PrivateKey().Address;
            var agent = new PrivateKey().Address;
            var contract = List.Empty.Add(patron.Serialize()).Add(true.Serialize());
            var states = new World(MockUtil.MockModernWorldState).SetLegacyState(agent.GetPledgeAddress(), contract);

            var action = new EndPledge
            {
                AgentAddress = invalidAgent ? new PrivateKey().Address : agent,
            };

            Assert.Throws(
                exc,
                () => action.Execute(
                    new ActionContext
                    {
                        Signer = invalidSigner ? new PrivateKey().Address : patron,
                        PreviousState = states,
                    }));
        }
    }
}
