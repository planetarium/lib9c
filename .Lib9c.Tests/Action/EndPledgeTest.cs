namespace Lib9c.Tests.Action
{
    using System;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
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
            IAccount states = new Account(MockState.Empty)
                .SetState(agent.GetPledgeAddress(), List.Empty.Add(patron.Serialize()).Add(true.Serialize()));
            var mead = Currencies.Mead;
            if (balance > 0)
            {
                states = states.MintAsset(context, agent, mead * balance);
            }

            var action = new EndPledge
            {
                AgentAddress = agent,
            };
            var nextState = action.Execute(new ActionContext
            {
                Signer = patron,
                PreviousState = states,
            });
            Assert.Equal(Null.Value, nextState.GetState(agent.GetPledgeAddress()));
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
            Address patron = new PrivateKey().Address;
            Address agent = new PrivateKey().Address;
            List contract = List.Empty.Add(patron.Serialize()).Add(true.Serialize());
            IAccount states = new Account(MockState.Empty).SetState(agent.GetPledgeAddress(), contract);

            var action = new EndPledge
            {
                AgentAddress = invalidAgent ? new PrivateKey().Address : agent,
            };

            Assert.Throws(exc, () => action.Execute(new ActionContext
            {
                Signer = invalidSigner ? new PrivateKey().Address : patron,
                PreviousState = states,
            }));
        }
    }
}
