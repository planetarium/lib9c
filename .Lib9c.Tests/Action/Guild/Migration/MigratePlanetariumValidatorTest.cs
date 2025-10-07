namespace Lib9c.Tests.Action.Guild.Migration
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using Lib9c.Action.Guild;
    using Lib9c.Action.Guild.Migration;
    using Lib9c.Action.Guild.Migration.LegacyModels;
    using Lib9c.Action.ValidatorDelegation;
    using Lib9c.Model.Guild;
    using Lib9c.Model.Stake;
    using Lib9c.Tests.Util;
    using Lib9c.TypedAddress;
    using Lib9c.ValidatorDelegation;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Consensus;
    using Xunit;

    // TODO: Remove this test class after the migration is completed.
    public class MigratePlanetariumValidatorTest : GuildTestBase
    {
        [Fact]
        public void Execute()
        {
            var guildAddress = AddressUtil.CreateGuildAddress();
            var validatorKey = new PrivateKey().PublicKey;
            var validatorAddress = validatorKey.Address;
            var power = 10_000_000_000_000_000_000;
            var guildGold = Currencies.GuildGold;
            var delegated = FungibleAssetValue.FromRawValue(guildGold, power);

            var world = EnsureLegacyPlanetariumValidator(
                World, guildAddress, validatorKey, power);

            var guildRepository = new GuildRepository(world, new ActionContext { });
            var validatorRepository = new ValidatorRepository(world, new ActionContext { });
            var guildDelegatee = guildRepository.GetDelegatee(validatorAddress);
            var validatorDelegatee = validatorRepository.GetDelegatee(validatorAddress);

            Assert.False(validatorDelegatee.IsActive);
            Assert.Equal(ValidatorDelegatee.InactiveDelegationPoolAddress, guildDelegatee.DelegationPoolAddress);
            Assert.Equal(ValidatorDelegatee.InactiveDelegationPoolAddress, validatorDelegatee.DelegationPoolAddress);
            Assert.Equal(delegated, world.GetBalance(ValidatorDelegatee.InactiveDelegationPoolAddress, Currencies.GuildGold));
            Assert.Equal(guildGold * 0, world.GetBalance(ValidatorDelegatee.ActiveDelegationPoolAddress, Currencies.GuildGold));

            var action = new MigratePlanetariumValidator();
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = new PrivateKey().Address,
            };
            world = action.Execute(actionContext);

            guildRepository = new GuildRepository(world, new ActionContext { });
            validatorRepository = new ValidatorRepository(world, new ActionContext { });
            guildDelegatee = guildRepository.GetDelegatee(validatorAddress);
            validatorDelegatee = validatorRepository.GetDelegatee(validatorAddress);

            Assert.True(validatorRepository.GetDelegatee(validatorAddress).IsActive);
            Assert.Equal(ValidatorDelegatee.ActiveDelegationPoolAddress, guildDelegatee.DelegationPoolAddress);
            Assert.Equal(ValidatorDelegatee.ActiveDelegationPoolAddress, validatorDelegatee.DelegationPoolAddress);
            Assert.Equal(delegated, world.GetBalance(ValidatorDelegatee.ActiveDelegationPoolAddress, Currencies.GuildGold));
            Assert.Equal(guildGold * 0, world.GetBalance(ValidatorDelegatee.InactiveDelegationPoolAddress, Currencies.GuildGold));

            Assert.Throws<InvalidOperationException>(() =>
            {
                var actionContext = new ActionContext
                {
                    PreviousState = world,
                    Signer = new PrivateKey().Address,
                };

                world = action.Execute(actionContext);
            });
        }

        private static IWorld EnsureLegacyPlanetariumValidator(
            IWorld world, GuildAddress guildAddress, PublicKey validatorKey, BigInteger power)
        {
            world = world.SetDelegationMigrationHeight(0L);

            var toDelegate = FungibleAssetValue.FromRawValue(Currencies.GuildGold, power);
            world = world
                .MintAsset(
                    new ActionContext { },
                    StakeState.DeriveAddress(validatorKey.Address),
                    toDelegate)
                .MintAsset(
                    new ActionContext { },
                    validatorKey.Address,
                    Currencies.Mead * 1);

            world = new PromoteValidator(validatorKey, toDelegate).Execute(new ActionContext
            {
                PreviousState = world,
                Signer = validatorKey.Address,
            });

            world = new MakeGuild(validatorKey.Address).Execute(new ActionContext
            {
                PreviousState = world,
                Signer = GuildConfig.PlanetariumGuildOwner,
            });

            world = world.SetValidatorSet(new ValidatorSet(
                new List<Validator>
                {
                    new Validator(validatorKey, power),
                }));

            return world;
        }
    }
}
