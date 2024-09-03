namespace Lib9c.Tests.Action.Guild
{
    using System;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action.Guild;
    using Nekoyume.Model.Guild;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
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

            var deserialized = new CancelGuildApplication();
            deserialized.LoadPlainValue(plainValue);
        }

        [Fact]
        public void Execute()
        {
            var privateKey = new PrivateKey();
            var signer = new AgentAddress(privateKey.Address);
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            var action = new CancelGuildApplication();
            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            var repository = new GuildRepository(world, new ActionContext());
            repository.MakeGuild(guildAddress, guildMasterAddress);
            Assert.Throws<InvalidOperationException>(
                () => action.Execute(new ActionContext
                {
                    PreviousState = repository.World,
                    Signer = signer,
                }));

            var otherAddress = AddressUtil.CreateAgentAddress();
            repository.ApplyGuild(otherAddress, guildAddress);

            // It should fail because other agent applied the guild but the signer didn't apply.
            Assert.Throws<InvalidOperationException>(
                () => action.Execute(new ActionContext
                {
                    PreviousState = repository.World,
                    Signer = signer,
                }));

            repository.ApplyGuild(signer, guildAddress);
            world = action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = signer,
            });

            repository.UpdateWorld(world);

            Assert.False(repository.TryGetGuildApplication(signer, out _));
        }
    }
}
