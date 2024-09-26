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

    public class RemoveGuildTest
    {
        [Fact]
        public void Serialization()
        {
            var action = new RemoveGuild();
            var plainValue = action.PlainValue;

            var deserialized = new RemoveGuild();
            deserialized.LoadPlainValue(plainValue);
        }

        [Fact]
        public void Execute_By_GuildMember()
        {
            var action = new RemoveGuild();

            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildMemberAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            var repository = new GuildRepository(world, new ActionContext());
            repository.MakeGuild(guildAddress, guildMasterAddress);

            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = guildMemberAddress,
            }));
        }

        [Fact]
        public void Execute_By_GuildMaster()
        {
            var action = new RemoveGuild();

            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            var repository = new GuildRepository(world, new ActionContext());
            repository.MakeGuild(guildAddress, guildMasterAddress);

            var changedWorld = action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = guildMasterAddress,
            });

            repository.UpdateWorld(changedWorld);
            Assert.False(repository.TryGetGuild(guildAddress, out _));
            Assert.Null(repository.GetJoinedGuild(guildMasterAddress));
        }

        [Fact]
        public void Execute_By_Other()
        {
            var action = new RemoveGuild();

            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var otherAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            var repository = new GuildRepository(world, new ActionContext());
            repository.MakeGuild(guildAddress, guildMasterAddress);

            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = otherAddress,
            }));
        }

        [Fact]
        public void ResetBannedAddresses()
        {
            var action = new RemoveGuild();

            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();
            var bannedAddress = AddressUtil.CreateAgentAddress();

            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            var repository = new GuildRepository(world, new ActionContext());
            repository.MakeGuild(guildAddress, guildMasterAddress);
            repository.Ban(guildAddress, guildMasterAddress, bannedAddress);

            Assert.True(repository.IsBanned(guildAddress, bannedAddress));

            world = action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = guildMasterAddress,
            });

            repository.UpdateWorld(world);
            Assert.False(repository.IsBanned(guildAddress, bannedAddress));
        }
    }
}