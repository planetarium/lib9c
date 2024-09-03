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

    public class RejectGuildApplicationTest
    {
        [Fact]
        public void Serialization()
        {
            var agentAddress = AddressUtil.CreateAgentAddress();
            var action = new RejectGuildApplication(agentAddress);
            var plainValue = action.PlainValue;

            var deserialized = new RejectGuildApplication();
            deserialized.LoadPlainValue(plainValue);
            Assert.Equal(agentAddress, deserialized.Target);
        }

        [Fact]
        public void Execute()
        {
            var appliedMemberAddress = AddressUtil.CreateAgentAddress();
            var nonAppliedMemberAddress = AddressUtil.CreateAgentAddress();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            var repository = new GuildRepository(world, new ActionContext());
            repository.MakeGuild(guildAddress, guildMasterAddress);
            repository.ApplyGuild(appliedMemberAddress, guildAddress);

            // These cases should fail because the member didn't apply the guild and
            // non-guild-master-addresses cannot reject the guild application.
            Assert.Throws<InvalidOperationException>(
                () => new RejectGuildApplication(nonAppliedMemberAddress).Execute(new ActionContext
                {
                    PreviousState = repository.World,
                    Signer = guildMasterAddress,
                }));
            Assert.Throws<InvalidOperationException>(
                () => new RejectGuildApplication(nonAppliedMemberAddress).Execute(new ActionContext
                {
                    PreviousState = repository.World,
                    Signer = appliedMemberAddress,
                }));
            Assert.Throws<InvalidOperationException>(
                () => new RejectGuildApplication(nonAppliedMemberAddress).Execute(new ActionContext
                {
                    PreviousState = repository.World,
                    Signer = nonAppliedMemberAddress,
                }));

            // These cases should fail because non-guild-master-addresses cannot reject the guild application.
            Assert.Throws<InvalidOperationException>(
                () => new RejectGuildApplication(appliedMemberAddress).Execute(new ActionContext
                {
                    PreviousState = repository.World,
                    Signer = appliedMemberAddress,
                }));
            Assert.Throws<InvalidOperationException>(
                () => new RejectGuildApplication(appliedMemberAddress).Execute(new ActionContext
                {
                    PreviousState = repository.World,
                    Signer = nonAppliedMemberAddress,
                }));

            world = new RejectGuildApplication(appliedMemberAddress).Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = guildMasterAddress,
            });

            repository.UpdateWorld(world);
            Assert.False(repository.TryGetGuildApplication(appliedMemberAddress, out _));
            Assert.Null(repository.GetJoinedGuild(appliedMemberAddress));
        }
    }
}
