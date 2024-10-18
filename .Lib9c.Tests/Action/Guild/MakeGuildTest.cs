namespace Lib9c.Tests.Action.Guild
{
    using System;
    using System.Collections.Generic;
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

    public class MakeGuildTest : GuildTestBase
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
                GuildConfig.PlanetariumGuildOwner,
                false,
            },
        };

        [Fact]
        public void Serialization()
        {
            var action = new MakeGuild();
            var plainValue = action.PlainValue;

            var deserialized = new MakeGuild();
            deserialized.LoadPlainValue(plainValue);
        }

        [Fact]
        public void Execute()
        {
            IWorld world = World;
            var validatorPrivateKey = new PrivateKey();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();
            world = EnsureToMintAsset(world, validatorPrivateKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorPrivateKey.PublicKey);
            var action = new MakeGuild(guildAddress, validatorPrivateKey.Address);

            world = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = guildMasterAddress,
            });

            var repository = new GuildRepository(world, new ActionContext());
            var guildParticipant = repository.GetGuildParticipant(guildMasterAddress);
            var guild = repository.GetGuild(guildParticipant.GuildAddress);
            Assert.Equal(guildAddress, guild.Address);
            Assert.Equal(guildMasterAddress, guild.GuildMasterAddress);
        }
    }
}
