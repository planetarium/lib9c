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

    public class RemoveGuildTest : GuildTestBase
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

            var removeGuild = new RemoveGuild();
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = guildMasterAddress,
            };
            world = removeGuild.Execute(actionContext);

            var repository = new GuildRepository(world, actionContext);
            Assert.Throws<FailedLoadStateException>(() => repository.GetGuild(guildAddress));
        }

        [Fact]
        public void Execute_ByGuildMember_Throw()
        {
            var validatorKey = new PrivateKey();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildMemberAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey.PublicKey);
            world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
            world = EnsureToJoinGuild(world, guildAddress, guildMemberAddress);

            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = guildMemberAddress,
            };
            var removeGuild = new RemoveGuild();

            Assert.Throws<InvalidOperationException>(() => removeGuild.Execute(actionContext));
        }

        [Fact]
        public void Execute_WhenDelegationExists_Throw()
        {
            var validatorKey = new PrivateKey();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey.PublicKey);
            world = EnsureToMintAsset(world, guildMasterAddress, GG * 100);
            world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);

            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = guildMasterAddress,
            };
            var removeGuild = new RemoveGuild();

            Assert.Throws<InvalidOperationException>(() => removeGuild.Execute(actionContext));
        }

        [Fact]
        public void Execute_ByNonGuildMember_Throw()
        {
            var validatorKey = new PrivateKey();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var otherAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey.PublicKey);
            world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);

            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = otherAddress,
            };
            var removeGuild = new RemoveGuild();

            Assert.Throws<InvalidOperationException>(() => removeGuild.Execute(actionContext));
        }

        [Fact]
        public void Execute_ResetBannedAddresses()
        {
            var validatorKey = new PrivateKey();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();
            var bannedAddress = AddressUtil.CreateAgentAddress();

            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey.PublicKey);
            world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
            world = EnsureToJoinGuild(world, guildAddress, bannedAddress);
            world = EnsureToBanGuildMember(world, guildAddress, guildMasterAddress, bannedAddress);

            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = guildMasterAddress,
            };
            var removeGuild = new RemoveGuild();
            world = removeGuild.Execute(actionContext);

            var repository = new GuildRepository(world, actionContext);
            Assert.False(repository.IsBanned(guildAddress, bannedAddress));
        }
    }
}
