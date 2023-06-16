namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Libplanet.State;
    using Nekoyume;
    using Nekoyume.Action;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class CreatePledgeTest
    {
        public CreatePledgeTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        [Fact]
        public void Execute()
        {
            var patronAddress = new PrivateKey().ToAddress();
            var mead = Currencies.Mead;
            var agentAddress = new PrivateKey().ToAddress();
            var pledgeAddress = agentAddress.GetPledgeAddress();
            IAccountStateDelta states = new State()
                .MintAsset(patronAddress, 4 * 500 * mead);

            var agentAddresses = new List<(Address, Address)>
            {
                (agentAddress, pledgeAddress),
            };
            for (int i = 0; i < 499; i++)
            {
                var address = new PrivateKey().ToAddress();
                agentAddresses.Add((address, address.GetPledgeAddress()));
            }

            var action = new CreatePledge
            {
                PatronAddress = patronAddress,
                Mead = RequestPledge.RefillMead,
                AgentAddresses = agentAddresses,
            };

            var nextState = action.Execute(new ActionContext
            {
                Signer = new PrivateKey().ToAddress(),
                PreviousStates = states,
            });

            Assert.Equal(0 * mead, nextState.GetBalance(patronAddress, mead));
            Assert.Equal(4 * mead, nextState.GetBalance(agentAddress, mead));
        }
    }
}
