namespace Lib9c.Tests.Action
{
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class BringEinheriTest
    {
        [Fact]
        public void Execute()
        {
            Currency mead = Currencies.Mead;
            IAccountStateDelta states = new State().MintAsset(Addresses.Valkyrie, 2 * mead);
            var address = new PrivateKey().ToAddress();
            var action = new BringEinheri
            {
                EinheriAddress = address,
            };

            Assert.Equal(0 * mead, states.GetBalance(address, mead));
            Assert.Equal(2 * mead, states.GetBalance(Addresses.Valkyrie, mead));

            var nextState = action.Execute(new ActionContext
            {
                Signer = Addresses.Valkyrie,
                PreviousStates = states,
            });
            var contract = Assert.IsType<List>(nextState.GetState(address.Derive(nameof(BringEinheri))));

            Assert.Equal(Addresses.Valkyrie, contract[0].ToAddress());
            Assert.False(contract[1].ToBoolean());
            Assert.Equal(1 * mead, nextState.GetBalance(address, mead));
            Assert.Equal(0 * mead, nextState.GetBalance(Addresses.Valkyrie, mead));
        }
    }
}
