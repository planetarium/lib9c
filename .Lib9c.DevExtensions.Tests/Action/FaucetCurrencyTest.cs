using Lib9c.DevExtensions.Action;
using Lib9c.Model.State;
using Lib9c.Module;
using Lib9c.Tests.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Libplanet.Types.Assets;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Lib9c.DevExtensions.Tests.Action
{
    public class FaucetCurrencyTest
    {
        private readonly IWorld _initialState;
        private readonly Address _agentAddress;
        private readonly Currency _ncg;
        private readonly Currency _crystal;

        public FaucetCurrencyTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

#pragma warning disable CS0618
            _ncg = Currency.Legacy("NCG", 2, null);
            _crystal = Currency.Legacy("CRYSTAL", 18, null);
#pragma warning restore CS0618

            _initialState = new World(MockWorldState.CreateModern()
                .SetBalance(GoldCurrencyState.Address, _ncg * int.MaxValue));

            var goldCurrencyState = new GoldCurrencyState(_ncg);
            _agentAddress = new PrivateKey().Address;
            var agentState = new AgentState(_agentAddress);

            _initialState = _initialState
                    .SetAgentState(_agentAddress, agentState)
                    .SetLegacyState(GoldCurrencyState.Address, goldCurrencyState.Serialize())
                ;
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(10, 0, 10, 0)]
        [InlineData(0, 10, 0, 10)]
        [InlineData(10, 10, 10, 10)]
        [InlineData(-10, 0, 0, 0)]
        [InlineData(0, -10, 0, 0)]
        public void Execute_FaucetCurrency(
            int faucetNcg,
            int faucetCrystal,
            int expectedNcg,
            int expectedCrystal
        )
        {
            var action = new FaucetCurrency
            {
                AgentAddress = _agentAddress,
                FaucetNcg = faucetNcg,
                FaucetCrystal = faucetCrystal,
            };
            var state = action.Execute(new ActionContext { PreviousState = _initialState, });
            var agentState = state.GetAgentState(_agentAddress);
            var expectedNcgAsset =
                new FungibleAssetValue(_ncg, expectedNcg, 0);
            var ncg = state.GetBalance(_agentAddress, state.GetGoldCurrency());
            Assert.Equal(expectedNcgAsset, ncg);

            var expectedCrystalAsset =
                new FungibleAssetValue(_crystal, expectedCrystal, 0);
            var crystal = state.GetBalance(_agentAddress, _crystal);
            Assert.Equal(expectedCrystalAsset, crystal);
        }
    }
}
