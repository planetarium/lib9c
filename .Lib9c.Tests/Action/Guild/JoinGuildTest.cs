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
    using Xunit;

    public class JoinGuildTest
    {
        [Fact]
        public void Serialization()
        {
            var guildAddress = AddressUtil.CreateGuildAddress();
            var action = new JoinGuild(guildAddress);
            var plainValue = action.PlainValue;

            var deserialized = new JoinGuild();
            deserialized.LoadPlainValue(plainValue);
            Assert.Equal(guildAddress, deserialized.GuildAddress);
        }

        [Fact]
        public void Execute()
        {
            var agentAddress = AddressUtil.CreateAgentAddress();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();
            var validatorAddress = new PrivateKey().Address;

            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            var repository = new GuildRepository(world, new ActionContext());
            repository.MakeGuild(guildAddress, guildMasterAddress, validatorAddress);
            repository.JoinGuild(guildAddress, agentAddress);

            var guild = repository.GetGuild(agentAddress);

            Assert.Equal(guildMasterAddress, guild.GuildMasterAddress);
            Assert.Equal(guildAddress, guild.Address);
        }
    }
}
