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

    public class BanGuildMemberTest : GuildTestBase
    {
        [Fact]
        public void Serialization()
        {
            var guildMemberAddress = AddressUtil.CreateAgentAddress();
            var action = new BanGuildMember(guildMemberAddress);
            var plainValue = action.PlainValue;

            var deserialized = new BanGuildMember();
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
            world = EnsureToJoinGuild(world, targetGuildMemberAddress, guildAddress);

            var banGuildMember = new BanGuildMember(targetGuildMemberAddress);
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = guildMasterAddress,
            };
            world = banGuildMember.Execute(actionContext);

            var repository = new GuildRepository(world, actionContext);
            Assert.True(repository.IsBanned(guildAddress, targetGuildMemberAddress));
            Assert.Null(repository.GetJoinedGuild(targetGuildMemberAddress));
        }

        // Expected use-case.
        [Fact]
        public void Ban_By_GuildMaster()
        {
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var otherGuildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildMemberAddress = AddressUtil.CreateAgentAddress();
            var otherGuildMemberAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();
            var otherGuildAddress = AddressUtil.CreateGuildAddress();

            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            var repository = new GuildRepository(world, new ActionContext());
            repository.MakeGuild(guildAddress, guildMasterAddress);
            repository.JoinGuild(guildAddress, guildMemberAddress);
            repository.MakeGuild(otherGuildAddress, otherGuildMasterAddress);
            repository.JoinGuild(otherGuildAddress, otherGuildMemberAddress);

            // Guild
            Assert.False(repository.IsBanned(guildAddress, guildMasterAddress));
            Assert.Equal(guildAddress, repository.GetJoinedGuild(guildMasterAddress));
            Assert.False(repository.IsBanned(guildAddress, guildMemberAddress));
            Assert.Equal(guildAddress, repository.GetJoinedGuild(guildMemberAddress));
            // Other guild
            Assert.False(repository.IsBanned(guildAddress, otherGuildMasterAddress));
            Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMasterAddress));
            Assert.False(repository.IsBanned(guildAddress, otherGuildMemberAddress));
            Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMemberAddress));

            var action = new BanGuildMember(guildMemberAddress);
            world = action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = guildMasterAddress,
            });

            // Guild
            repository.UpdateWorld(world);
            Assert.False(repository.IsBanned(guildAddress, guildMasterAddress));
            Assert.Equal(guildAddress, repository.GetJoinedGuild(guildMasterAddress));
            Assert.True(repository.IsBanned(guildAddress, guildMemberAddress));
            Assert.Null(repository.GetJoinedGuild(guildMemberAddress));
            // Other guild
            Assert.False(repository.IsBanned(guildAddress, otherGuildMasterAddress));
            Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMasterAddress));
            Assert.False(repository.IsBanned(guildAddress, otherGuildMemberAddress));
            Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMemberAddress));

            action = new BanGuildMember(otherGuildMasterAddress);
            world = action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = guildMasterAddress,
            });

            // Guild
            repository.UpdateWorld(world);
            Assert.False(repository.IsBanned(guildAddress, guildMasterAddress));
            Assert.Equal(guildAddress, repository.GetJoinedGuild(guildMasterAddress));
            Assert.True(repository.IsBanned(guildAddress, guildMemberAddress));
            Assert.Null(repository.GetJoinedGuild(guildMemberAddress));
            // Other guild
            Assert.True(repository.IsBanned(guildAddress, otherGuildMasterAddress));
            Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMasterAddress));
            Assert.False(repository.IsBanned(guildAddress, otherGuildMemberAddress));
            Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMemberAddress));

            action = new BanGuildMember(otherGuildMemberAddress);
            world = action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = guildMasterAddress,
            });

            // Guild
            repository.UpdateWorld(world);
            Assert.False(repository.IsBanned(guildAddress, guildMasterAddress));
            Assert.Equal(guildAddress, repository.GetJoinedGuild(guildMasterAddress));
            Assert.True(repository.IsBanned(guildAddress, guildMemberAddress));
            Assert.Null(repository.GetJoinedGuild(guildMemberAddress));
            // Other guild
            Assert.True(repository.IsBanned(guildAddress, otherGuildMasterAddress));
            Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMasterAddress));
            Assert.True(repository.IsBanned(guildAddress, otherGuildMemberAddress));
            Assert.Equal(otherGuildAddress, repository.GetJoinedGuild(otherGuildMemberAddress));

            action = new BanGuildMember(guildMasterAddress);
            // GuildMaster cannot ban itself.
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = guildMasterAddress,
            }));
        }

        [Fact]
        public void Ban_By_GuildMember()
        {
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var guildMemberAddress = AddressUtil.CreateAgentAddress();
            var otherAddress = AddressUtil.CreateAgentAddress();
            var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            var action = new BanGuildMember(targetGuildMemberAddress);

            IWorld world = new World(MockUtil.MockModernWorldState);
            var ncg = Currency.Uncapped("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            var repository = new GuildRepository(world, new ActionContext());
            repository.MakeGuild(guildAddress, guildMasterAddress);
            repository.JoinGuild(guildAddress, guildMemberAddress);
            repository.JoinGuild(guildAddress, targetGuildMemberAddress);

            // GuildMember tries to ban other guild member.
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = guildMemberAddress,
            }));

            // GuildMember tries to ban itself.
            action = new BanGuildMember(guildMemberAddress);
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = guildMemberAddress,
            }));

            action = new BanGuildMember(otherAddress);
            // GuildMember tries to ban other not joined to its guild.
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = guildMemberAddress,
            }));
        }

        [Fact]
        public void Ban_By_Other()
        {
            // NOTE: It assumes 'other' hasn't any guild. If 'other' has its own guild,
            //       it should be assumed as a guild master.
            var validatorKey = new PrivateKey();
            var guildMasterAddress = AddressUtil.CreateAgentAddress();
            var otherAddress = AddressUtil.CreateAgentAddress();
            var targetGuildMemberAddress = AddressUtil.CreateAgentAddress();
            var guildAddress = AddressUtil.CreateGuildAddress();

            IWorld world = World;
            world = EnsureToMintAsset(world, validatorKey.Address, GG * 100);
            world = EnsureToCreateValidator(world, validatorKey.PublicKey);
            world = EnsureToMakeGuild(world, guildAddress, guildMasterAddress, validatorKey.Address);
            world = EnsureToJoinGuild(world, targetGuildMemberAddress, guildAddress);

            var repository = new GuildRepository(world, new ActionContext());
            repository.MakeGuild(guildAddress, guildMasterAddress);
            repository.JoinGuild(guildAddress, targetGuildMemberAddress);

            // Other tries to ban GuildMember.
            var action = new BanGuildMember(targetGuildMemberAddress);
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = otherAddress,
            }));

            // Other tries to ban GuildMaster.
            action = new BanGuildMember(guildMasterAddress);
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = otherAddress,
            }));
        }
    }
}
