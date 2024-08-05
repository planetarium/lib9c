namespace Lib9c.Tests.Action.Guild
{
    using System;
    using System.Collections.Generic;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.Loader;
    using Nekoyume.Module.Guild;
    using Nekoyume.TypedAddress;
    using Xunit;

    public class MakeGuildTest
    {
        public static IEnumerable<object[]> TestCases => new[]
        {
            new object[]
            {
                AddressUtil.CreateAgentAddress(),
                // TODO: Update to false when Guild features are enabled.
                true,
            },
            new object[]
            {
                new AgentAddress(MeadConfig.PatronAddress),
                false,
            },
        };

        [Fact]
        public void Serialization()
        {
            var action = new MakeGuild();
            var plainValue = action.PlainValue;

            var actionLoader = new NCActionLoader();
            var loadedRaw = actionLoader.LoadAction(0, plainValue);
            Assert.IsType<MakeGuild>(loadedRaw);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void Execute(AgentAddress guildMasterAddress, bool fail)
        {
            var action = new MakeGuild();
            IWorld world = new World(MockUtil.MockModernWorldState);

            if (fail)
            {
                Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = guildMasterAddress,
                }));
            }
            else
            {
                world = action.Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = guildMasterAddress,
                });

                var guildAddress = world.GetJoinedGuild(guildMasterAddress);
                Assert.NotNull(guildAddress);
                Assert.True(world.TryGetGuild(guildAddress.Value, out var guild));
                Assert.Equal(guildMasterAddress, guild.GuildMasterAddress);
            }
        }
    }
}
