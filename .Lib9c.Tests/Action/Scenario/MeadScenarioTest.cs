namespace Lib9c.Tests.Action.Scenario
{
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Xunit;

    public class MeadScenarioTest
    {
        [Fact]
        public void Contract()
        {
            Currency mead = Currencies.Mead;
            IAccountStateDelta states =
                new State().MintAsset(Addresses.Heidrun, 100 * mead);
            var reincarnation = new Reincarnation();
            var states1 = reincarnation.Execute(new ActionContext
            {
                Signer = Addresses.Valkyrie,
                PreviousStates = states,
            });
            Assert.Equal(90 * mead, states1.GetBalance(Addresses.Heidrun, mead));
            Assert.Equal(10 * mead, states1.GetBalance(Addresses.Valkyrie, mead));

            var agentAddress = new PrivateKey().ToAddress();
            var bringEinheri = new BringEinheri
            {
                EinheriAddress = agentAddress,
            };
            var states2 = bringEinheri.Execute(new ActionContext
            {
                Signer = Addresses.Valkyrie,
                PreviousStates = states1,
            });
            Assert.Equal(90 * mead, states2.GetBalance(Addresses.Heidrun, mead));
            Assert.Equal(9 * mead, states2.GetBalance(Addresses.Valkyrie, mead));
            Assert.Equal(1 * mead, states2.GetBalance(agentAddress, mead));

            var takeSides = new TakeSides
            {
                ValkyrieAddress = Addresses.Valkyrie,
            };
            var states3 = takeSides.Execute(new ActionContext
            {
                Signer = agentAddress,
                PreviousStates = states2,
            });
            Assert.Equal(90 * mead, states3.GetBalance(Addresses.Heidrun, mead));
            Assert.Equal(9 * mead, states3.GetBalance(Addresses.Valkyrie, mead));
            Assert.Equal(1 * mead, states3.GetBalance(agentAddress, mead));

            // release and return einheri mead
            var releaseEinheri = new ReleaseEinheri
            {
                EinheriAddress = agentAddress,
            };
            var states4 = releaseEinheri.Execute(new ActionContext
            {
                Signer = Addresses.Valkyrie,
                PreviousStates = states3,
            });
            Assert.Equal(90 * mead, states4.GetBalance(Addresses.Heidrun, mead));
            Assert.Equal(10 * mead, states4.GetBalance(Addresses.Valkyrie, mead));
            Assert.Equal(0 * mead, states4.GetBalance(agentAddress, mead));

            // re-contract with Bencodex.Null
            var states5 = bringEinheri.Execute(new ActionContext
            {
                Signer = Addresses.Valkyrie,
                PreviousStates = states4,
            });
            Assert.Equal(90 * mead, states5.GetBalance(Addresses.Heidrun, mead));
            Assert.Equal(9 * mead, states5.GetBalance(Addresses.Valkyrie, mead));
            Assert.Equal(1 * mead, states5.GetBalance(agentAddress, mead));

            var states6 = takeSides.Execute(new ActionContext
            {
                Signer = agentAddress,
                PreviousStates = states5,
            });
            Assert.Equal(90 * mead, states6.GetBalance(Addresses.Heidrun, mead));
            Assert.Equal(9 * mead, states6.GetBalance(Addresses.Valkyrie, mead));
            Assert.Equal(1 * mead, states6.GetBalance(agentAddress, mead));
        }
    }
}
