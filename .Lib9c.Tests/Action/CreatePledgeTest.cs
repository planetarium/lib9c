namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Lib9c.Action;
    using Lib9c.Action.Guild;
    using Lib9c.Model.Guild;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Lib9c.Module.Guild;
    using Lib9c.Tests.Util;
    using Lib9c.TypedAddress;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class CreatePledgeTest
    {
        public CreatePledgeTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        [Theory]
        [InlineData(true, false, null)]
        [InlineData(true, true, null)]
        [InlineData(false, false, typeof(PermissionDeniedException))]
        public void Execute(bool admin, bool plPatron, Type exc)
        {
            var validatorKey = new PrivateKey();
            var adminAddress = new PrivateKey().Address;
            var poolAddress = new PrivateKey().Address;
            var adminState = new AdminState(adminAddress, 150L);
            var patronAddress = plPatron
                ? MeadConfig.PatronAddress
                : new PrivateKey().Address;
            var mead = Currencies.Mead;
            var pledgedAddress = new PrivateKey().Address;
            var pledgeAddress = pledgedAddress.GetPledgeAddress();
            var agentAddress = new AgentAddress(pledgedAddress);
            var context = new ActionContext();
            var (tables, agentAddr, avatarAddr, states) = InitializeUtil.InitializeStates();
            states = states
                .SetLegacyState(Addresses.Admin, adminState.Serialize())
                .MintAsset(context, patronAddress, 4 * 500 * mead);

            states = Lib9c.Tests.Util.DelegationUtil.EnsureValidatorPromotionReady(
                states,
                validatorKey.PublicKey,
                0L
            );

            var agentAddresses = new List<(Address, Address)>
            {
                (pledgedAddress, pledgeAddress),
            };
            for (var i = 0; i < 499; i++)
            {
                var address = new PrivateKey().Address;
                agentAddresses.Add((address, address.GetPledgeAddress()));
            }

            var action = new CreatePledge
            {
                PatronAddress = patronAddress,
                Mead = RequestPledge.DefaultRefillMead,
                AgentAddresses = agentAddresses,
            };

            var singer = admin ? adminAddress : poolAddress;
            var actionContext = new ActionContext
            {
                Signer = singer,
                PreviousState = states,
                Miner = validatorKey.Address,
            };

            if (exc is null)
            {
                var nextState = action.Execute(actionContext);
                Assert.Equal(0 * mead, nextState.GetBalance(patronAddress, mead));
                Assert.Equal(4 * mead, nextState.GetBalance(pledgedAddress, mead));

                var repository = new GuildRepository(nextState, context);
                var planetariumGuildOwner = GuildConfig.PlanetariumGuildOwner;
                var guildAddress = repository.GetJoinedGuild(planetariumGuildOwner);
                Assert.NotNull(guildAddress);
                Assert.True(repository.TryGetGuild(guildAddress.Value, out var guild));
                Assert.Equal(planetariumGuildOwner, guild.GuildMasterAddress);
                if (!plPatron)
                {
                    Assert.Null(repository.GetJoinedGuild(agentAddress));
                }
                else
                {
                    var joinedGuildAddress =
                        Assert.IsType<GuildAddress>(
                            repository.GetJoinedGuild(agentAddress));
                    Assert.True(repository.TryGetGuild(joinedGuildAddress, out var joinedGuild));
                    Assert.Equal(
                        GuildConfig.PlanetariumGuildOwner,
                        joinedGuild.GuildMasterAddress
                    );
                }
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(actionContext));
            }
        }
    }
}
