namespace Lib9c.Tests.Action.Guild
{
    using System;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Guild;
    using Nekoyume.Model.Guild;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.Module.Guild;
    using Xunit;

    public class QuitGuildTest : GuildTestBase
    {
        [Fact]
        public void Serialization()
        {
            var action = new QuitGuild();
            var plainValue = action.PlainValue;

            var deserialized = new QuitGuild();
            deserialized.LoadPlainValue(plainValue);
        }

        [Fact]
        public void Execute()
        {
            var validatorKey = new PrivateKey();
            var agentAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();

            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey.PublicKey);
            world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
            world = EnsureToJoinGuild(world, guildAddress, agentAddress);

            var quitGuild = new QuitGuild();
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = agentAddress,
            };
            world = quitGuild.Execute(actionContext);

            var repository = new GuildRepository(world, actionContext);
            Assert.Throws<FailedLoadStateException>(
                () => repository.GetGuildParticipant(agentAddress));
        }
    }
}
