namespace Lib9c.Tests.Action.Guild
{
    using System;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.Loader;
    using Nekoyume.Module.Guild;
    using Nekoyume.TypedAddress;
    using Xunit;

    public class CancelGuildApplicationTest
    {
        [Fact]
        public void Serialization()
        {
            var action = new CancelGuildApplication();
            var plainValue = action.PlainValue;

            var actionLoader = new NCActionLoader();
            var loadedRaw = actionLoader.LoadAction(0, plainValue);
            Assert.IsType<CancelGuildApplication>(loadedRaw);
        }

        [Fact]
        public void Execute()
        {
            var privateKey = new PrivateKey();
            var signer = new AgentAddress(privateKey.Address);
            var action = new CancelGuildApplication();
            IWorld world = new World(MockUtil.MockModernWorldState);
            Assert.Throws<InvalidOperationException>(
                () => action.Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = signer,
                }));

            var guildAddress = AddressUtil.CreateGuildAddress();
            var otherAddress = AddressUtil.CreateAgentAddress();
            world = world.ApplyGuild(otherAddress, guildAddress);

            // It should fail because other agent applied the guild but the signer didn't apply.
            Assert.Throws<InvalidOperationException>(
                () => action.Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = signer,
                }));

            world = world.ApplyGuild(signer, guildAddress);
            world = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = signer,
            });

            Assert.False(world.TryGetGuildApplication(signer, out _));
        }
    }
}
