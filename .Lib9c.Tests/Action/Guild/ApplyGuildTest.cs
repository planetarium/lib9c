namespace Lib9c.Tests.Action.Guild
{
    using System;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action.Guild;
    using Nekoyume.Model.Guild;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.Module.Guild;
    using Xunit;

    public class ApplyGuildTest
    {
        [Fact]
        public void Serialization()
        {
            var guildAddress = AddressUtil.CreateGuildAddress();
            var action = new ApplyGuild(guildAddress);
            var plainValue = action.PlainValue;

            var deserialized = new ApplyGuild();
            deserialized.LoadPlainValue(plainValue);
            Assert.Equal(guildAddress, deserialized.GuildAddress);
        }

        [Fact]
        public void Execute()
        {
            var agentAddress = AddressUtil.CreateAgentAddress();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();
            var action = new ApplyGuild(guildAddress);

            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            var repository = new GuildRepository(world, new ActionContext());
            repository.MakeGuild(guildAddress, guildMasterAddress);
            repository.JoinGuild(guildAddress, guildMasterAddress);
            var bannedRepository = new GuildRepository(repository.World, new ActionContext());
            bannedRepository.Ban(guildAddress, guildMasterAddress, agentAddress);

            // This case should fail because the agent is banned by the guild.
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = bannedRepository.World,
                Signer = agentAddress,
            }));

            world = action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = agentAddress,
            });

            repository.UpdateWorld(world);
            Assert.True(repository.TryGetGuildApplication(agentAddress, out var application));
            Assert.Equal(guildAddress, application.GuildAddress);
        }
    }
}
