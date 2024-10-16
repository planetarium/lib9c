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

    public class MoveGuildTest
    {
        [Fact]
        public void Serialization()
        {
            var guildAddress = AddressUtil.CreateGuildAddress();
            var action = new MoveGuild(guildAddress);
            var plainValue = action.PlainValue;

            var deserialized = new MoveGuild();
            deserialized.LoadPlainValue(plainValue);
            Assert.Equal(guildAddress, deserialized.GuildAddress);
        }

        [Fact]
        public void Execute()
        {
            var agentAddress = AddressUtil.CreateAgentAddress();
            var guildMasterAddress1 = AddressUtil.CreateAgentAddress();
            var guildMasterAddress2 = AddressUtil.CreateAgentAddress();
            var guildAddress1 = AddressUtil.CreateGuildAddress();
            var guildAddress2 = AddressUtil.CreateGuildAddress();
            var validatorAddress1 = new PrivateKey().Address;
            var validatorAddress2 = new PrivateKey().Address;

            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            var repository = new GuildRepository(world, new ActionContext());
            repository.MakeGuild(guildAddress1, guildMasterAddress1, validatorAddress1);
            repository.MakeGuild(guildAddress2, guildMasterAddress2, validatorAddress2);
            repository.JoinGuild(guildAddress1, agentAddress);
            var guild1 = repository.GetGuild(agentAddress);

            repository.MoveGuild(agentAddress, guildAddress2);

            var guild2 = repository.GetGuild(agentAddress);

            Assert.NotEqual(guild1.Address, guild2.Address);
            Assert.Equal(guildMasterAddress2, guild2.GuildMasterAddress);
            Assert.Equal(guildAddress2, guild2.Address);
        }
    }
}
