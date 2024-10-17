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

    public class MoveGuildTest : GuildTestBase
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
            var validatorKey1 = new PrivateKey();
            var validatorKey2 = new PrivateKey();
            var agentAddress = AddressUtil.CreateAgentAddress();
            var guildMasterAddress1 = AddressUtil.CreateAgentAddress();
            var guildMasterAddress2 = AddressUtil.CreateAgentAddress();
            var guildAddress1 = AddressUtil.CreateGuildAddress();
            var guildAddress2 = AddressUtil.CreateGuildAddress();

            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey1.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey1.PublicKey);
            world = EnsureToMintAsset(world, validatorKey2.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey2.PublicKey);
            world = EnsureToMakeGuild(world, guildAddress1, guildMasterAddress1, validatorKey1.Address);
            world = EnsureToMakeGuild(world, guildAddress2, guildMasterAddress2, validatorKey2.Address);
            world = EnsureToJoinGuild(world, agentAddress, guildAddress1);

            var moveGuild = new MoveGuild(guildAddress2);
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = agentAddress,
            };
            world = moveGuild.Execute(actionContext);

            var repository = new GuildRepository(world, actionContext);
            var guildParticipant = repository.GetGuildParticipant(agentAddress);

            Assert.Equal(guildAddress2, guildParticipant.GuildAddress);
        }
    }
}
