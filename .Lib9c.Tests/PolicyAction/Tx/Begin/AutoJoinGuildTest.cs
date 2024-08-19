namespace Lib9c.Tests.PolicyAction.Tx.Begin
{
    using System;
    using Bencodex.Types;
    using Lib9c.Tests.Action;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Guild;
    using Nekoyume.Extensions;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.Module.Guild;
    using Nekoyume.PolicyAction.Tx.Begin;
    using Nekoyume.TypedAddress;
    using Xunit;

    public class AutoJoinGuildTest
    {
        [Fact]
        public void RunAsPolicyActionOnly()
        {
            Assert.Throws<InvalidOperationException>(() => new AutoJoinGuild().Execute(
                new ActionContext
                {
                    IsPolicyAction = false,
                }));
        }

        [Fact]
        public void Execute_When_WithPledgeContract()
        {
            var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
            var guildAddress = AddressUtil.CreateGuildAddress();
            var agentAddress = AddressUtil.CreateAgentAddress();
            var pledgeAddress = agentAddress.GetPledgeAddress();
            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMasterAddress)
                .SetLegacyState(pledgeAddress, new List(
                    MeadConfig.PatronAddress.Serialize(),
                    true.Serialize(),
                    RequestPledge.DefaultRefillMead.Serialize()));

            Assert.Null(world.GetJoinedGuild(agentAddress));
            var action = new AutoJoinGuild();
            world = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = agentAddress,
                IsPolicyAction = true,
            });

            var joinedGuildAddress = Assert.IsType<GuildAddress>(world.GetJoinedGuild(agentAddress));
            Assert.True(world.TryGetGuild(joinedGuildAddress, out var guild));
            Assert.Equal(GuildConfig.PlanetariumGuildOwner, guild.GuildMasterAddress);
        }

        [Fact]
        public void Execute_When_WithoutPledgeContract()
        {
            var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
            var guildAddress = AddressUtil.CreateGuildAddress();
            var agentAddress = AddressUtil.CreateAgentAddress();
            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .MakeGuild(guildAddress, guildMasterAddress)
                .JoinGuild(guildAddress, guildMasterAddress);

            Assert.Null(world.GetJoinedGuild(agentAddress));
            var action = new AutoJoinGuild();
            world = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = agentAddress,
                IsPolicyAction = true,
            });

            Assert.Null(world.GetJoinedGuild(agentAddress));
        }

        [Fact]
        public void Execute_When_WithoutGuildYet()
        {
            var agentAddress = AddressUtil.CreateAgentAddress();
            var pledgeAddress = agentAddress.GetPledgeAddress();
            var world = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(pledgeAddress, new List(
                    MeadConfig.PatronAddress.Serialize(),
                    true.Serialize(),
                    RequestPledge.DefaultRefillMead.Serialize()));

            Assert.Null(world.GetJoinedGuild(agentAddress));
            var action = new AutoJoinGuild();
            world = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = agentAddress,
                IsPolicyAction = true,
            });

            Assert.Null(world.GetJoinedGuild(agentAddress));
        }
    }
}
