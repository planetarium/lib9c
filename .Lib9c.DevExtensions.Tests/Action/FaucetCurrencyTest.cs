using Lib9c.DevExtensions.Action;
using Lib9c.Tests.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Serilog;
using System.Collections.Immutable;
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

            _initialState = new MockWorld(
                new MockWorldState(
                    ImmutableDictionary<Address, IAccount>.Empty.Add(
                        ReservedAddresses.LegacyAccount,
                        new MockAccount(
                            new MockAccountState()
                                .AddBalance(GoldCurrencyState.Address, _ncg * int.MaxValue)))));

            var goldCurrencyState = new GoldCurrencyState(_ncg);
            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);

            _initialState = AgentModule.SetAgentState(_initialState, _agentAddress, agentState);
            _initialState = LegacyModule.SetState(
                _initialState,
                GoldCurrencyState.Address,
                goldCurrencyState.Serialize());
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
            var world = action
                .Execute(
                    new ActionContext { PreviousState = _initialState });
            AgentState agentState = AgentModule.GetAgentState(world, _agentAddress);
            FungibleAssetValue expectedNcgAsset =
                new FungibleAssetValue(_ncg, expectedNcg, 0);
            FungibleAssetValue ncg = LegacyModule.GetBalance(
                world,
                _agentAddress,
                LegacyModule.GetGoldCurrency(world));
            Assert.Equal(expectedNcgAsset, ncg);

            FungibleAssetValue expectedCrystalAsset =
                new FungibleAssetValue(_crystal, expectedCrystal, 0);
            FungibleAssetValue crystal = LegacyModule.GetBalance(world, _agentAddress, _crystal);
            Assert.Equal(expectedCrystalAsset, crystal);
        }
    }
}
