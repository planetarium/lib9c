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

    public class UnbanGuildMemberTest : GuildTestBase
    {
        [Fact]
        public void Serialization()
        {
            var guildMemberAddress = new PrivateKey().Address;
            var action = new UnbanGuildMember(guildMemberAddress);
            var plainValue = action.PlainValue;

            var deserialized = new UnbanGuildMember();
            deserialized.LoadPlainValue(plainValue);
            Assert.Equal(guildMemberAddress, deserialized.Target);
        }

        [Fact]
        public void Execute()
        {
            var validatorKey = new PrivateKey();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey.PublicKey);
            world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
            world = EnsureToJoinGuild(world, guildAddress, targetGuildMemberAddress);
            world = EnsureToBanGuildMember(world, guildAddress, guildMasterAddress, targetGuildMemberAddress);

            var unbanGuildMember = new UnbanGuildMember(targetGuildMemberAddress);
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = guildMasterAddress,
            };
            world = unbanGuildMember.Execute(actionContext);

            var repository = new GuildRepository(world, actionContext);
            Assert.False(repository.IsBanned(guildAddress, targetGuildMemberAddress));
            Assert.Null(repository.GetJoinedGuild(targetGuildMemberAddress));
        }

        [Fact]
        public void Unban_By_GuildMember()
        {
            var validatorKey = new PrivateKey();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildMemberAddress = AddressUtil.CreateAgentAddress();
            var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            var action = new UnbanGuildMember(targetGuildMemberAddress);

            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey.PublicKey);
            world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
            world = EnsureToJoinGuild(world, guildAddress, guildMemberAddress);
            world = EnsureToBanGuildMember(world, guildAddress, guildMasterAddress, targetGuildMemberAddress);

            var repository = new GuildRepository(world, new ActionContext());

            // GuildMember tries to ban other guild member.
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = guildMemberAddress,
            }));

            // GuildMember tries to ban itself.
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = targetGuildMemberAddress,
            }));
        }

        [Fact]
        public void Unban_By_GuildMaster()
        {
            var validatorKey = new PrivateKey();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            var action = new UnbanGuildMember(targetGuildMemberAddress);

            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey.PublicKey);
            world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
            world = EnsureToJoinGuild(world, guildAddress, targetGuildMemberAddress);
            world = EnsureToBanGuildMember(world, guildAddress, guildMasterAddress, targetGuildMemberAddress);

            var repository = new GuildRepository(world, new ActionContext());

            Assert.True(repository.IsBanned(guildAddress, targetGuildMemberAddress));
            Assert.Null(repository.GetJoinedGuild(targetGuildMemberAddress));

            world = action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = guildMasterAddress,
            });

            repository.UpdateWorld(world);
            Assert.False(repository.IsBanned(guildAddress, targetGuildMemberAddress));
            Assert.Null(repository.GetJoinedGuild(targetGuildMemberAddress));
        }
    }
}
