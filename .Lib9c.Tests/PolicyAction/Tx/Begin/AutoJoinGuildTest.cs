namespace Lib9c.Tests.PolicyAction.Tx.Begin
{
    using System;
    using Bencodex.Types;
    using Lib9c.Tests.Action;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Guild;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Guild;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.Module.Guild;
    using Nekoyume.Module.ValidatorDelegation;
    using Nekoyume.PolicyAction.Tx.Begin;
    using Nekoyume.TypedAddress;
    using Nekoyume.ValidatorDelegation;
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
            var validatorKey = new PrivateKey();
            var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
            var guildAddress = AddressUtil.CreateGuildAddress();
            var agentAddress = AddressUtil.CreateAgentAddress();
            var pledgeAddress = agentAddress.GetPledgeAddress();
            IWorld world = new World(MockUtil.MockModernWorldState);
            var goldCurrencyState = new GoldCurrencyState(Currencies.GuildGold);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            world = world.MintAsset(new ActionContext(), validatorKey.Address, Currencies.GuildGold * 100);
            var validatorRepository = new ValidatorRepository(world, new ActionContext
            {
                Signer = validatorKey.Address,
            });
            validatorRepository.CreateValidatorDelegatee(validatorKey.PublicKey, 10);
            world = validatorRepository.World;

            var repository = new GuildRepository(world, new ActionContext
            {
                Signer = guildMasterAddress,
            });
            repository.MakeGuild(guildAddress, validatorKey.Address);
            repository.UpdateWorld(repository.World.SetLegacyState(pledgeAddress, new List(
                MeadConfig.PatronAddress.Serialize(),
                true.Serialize(),
                RequestPledge.DefaultRefillMead.Serialize())));

            Assert.Null(repository.GetJoinedGuild(agentAddress));
            var action = new AutoJoinGuild();
            world = action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = agentAddress,
                IsPolicyAction = true,
            });

            repository.UpdateWorld(world);
            var joinedGuildAddress = Assert.IsType<GuildAddress>(repository.GetJoinedGuild(agentAddress));
            Assert.True(repository.TryGetGuild(joinedGuildAddress, out var guild));
            Assert.Equal(GuildConfig.PlanetariumGuildOwner, guild.GuildMasterAddress);
        }

        [Fact]
        public void Execute_When_WithoutPledgeContract()
        {
            var validatorKey = new PrivateKey();
            var guildMasterAddress = GuildConfig.PlanetariumGuildOwner;
            var guildAddress = AddressUtil.CreateGuildAddress();
            var agentAddress = AddressUtil.CreateAgentAddress();
            IWorld world = new World(MockUtil.MockModernWorldState);
            var goldCurrencyState = new GoldCurrencyState(Currencies.GuildGold);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            world = world.MintAsset(new ActionContext(), validatorKey.Address, Currencies.GuildGold * 100);
            var validatorRepository = new ValidatorRepository(world, new ActionContext
            {
                Signer = validatorKey.Address,
            });
            validatorRepository.CreateValidatorDelegatee(validatorKey.PublicKey, 10);
            world = validatorRepository.World;

            var repository = new GuildRepository(world, new ActionContext());

            Assert.Null(repository.GetJoinedGuild(agentAddress));
            var action = new AutoJoinGuild();
            world = action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = agentAddress,
                IsPolicyAction = true,
            });

            repository.UpdateWorld(world);
            Assert.Null(repository.GetJoinedGuild(agentAddress));
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

            var repository = new GuildRepository(world, new ActionContext());
            Assert.Null(repository.GetJoinedGuild(agentAddress));
            var action = new AutoJoinGuild();
            world = action.Execute(new ActionContext
            {
                PreviousState = repository.World,
                Signer = agentAddress,
                IsPolicyAction = true,
            });

            repository.UpdateWorld(world);
            Assert.Null(repository.GetJoinedGuild(agentAddress));
        }
    }
}
