namespace Lib9c.Tests.Action.ValidatorDelegation
{
    using System;
    using System.Linq;
    using System.Numerics;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action.ValidatorDelegation;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.ValidatorDelegation;
    using Xunit;

    public class SetValidatorCommissionTest
    {
        [Fact]
        public void Serialization()
        {
            var address = new PrivateKey().Address;
            BigInteger commissionPercentage = 10;
            var action = new SetValidatorCommission(address, commissionPercentage);
            var plainValue = action.PlainValue;

            var deserialized = new SetValidatorCommission();
            deserialized.LoadPlainValue(plainValue);
            Assert.Equal(address, deserialized.ValidatorDelegatee);
            Assert.Equal(commissionPercentage, deserialized.CommissionPercentage);
        }

        [Fact]
        public void Execute()
        {
            IWorld world = new World(MockUtil.MockModernWorldState);
            var context = new ActionContext { };
            var ncg = Currency.Uncapped("NCG", 2, null);
            // TODO: Use Currencies.GuildGold when it's available.
            // var gg = Currencies.GuildGold;
            var gg = ncg;
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            var validatorPublicKey = new PrivateKey().PublicKey;
            world = world.MintAsset(context, validatorPublicKey.Address, gg * 100);
            var promoteFAV = gg * 10;
            world = new PromoteValidator(validatorPublicKey, promoteFAV).Execute(new ActionContext
            {
                PreviousState = world,
                Signer = validatorPublicKey.Address,
            });

            world = new SetValidatorCommission(validatorPublicKey.Address, 11).Execute(new ActionContext
            {
                PreviousState = world,
                Signer = validatorPublicKey.Address,
            });

            var repository = new ValidatorRepository(world, context);
            var validator = repository.GetValidatorDelegatee(validatorPublicKey.Address);
            Assert.Equal(11, validator.CommissionPercentage);
        }

        [Fact]
        public void CannotExceedMaxPercentage()
        {
            IWorld world = new World(MockUtil.MockModernWorldState);
            var context = new ActionContext { };
            var ncg = Currency.Uncapped("NCG", 2, null);
            // TODO: Use Currencies.GuildGold when it's available.
            // var gg = Currencies.GuildGold;
            var gg = ncg;
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            var validatorPublicKey = new PrivateKey().PublicKey;
            world = world.MintAsset(context, validatorPublicKey.Address, gg * 100);
            var promoteFAV = gg * 10;
            world = new PromoteValidator(validatorPublicKey, promoteFAV).Execute(new ActionContext
            {
                PreviousState = world,
                Signer = validatorPublicKey.Address,
            });

            foreach (var percentage in Enumerable.Range(11, 10))
            {
                world = new SetValidatorCommission(validatorPublicKey.Address, percentage).Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = validatorPublicKey.Address,
                });
            }

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SetValidatorCommission(validatorPublicKey.Address, 31).Execute(new ActionContext
            {
                PreviousState = world,
                Signer = validatorPublicKey.Address,
            }));
        }

        [Fact]
        public void CannotExceedMaxChange()
        {
            IWorld world = new World(MockUtil.MockModernWorldState);
            var context = new ActionContext { };
            var ncg = Currency.Uncapped("NCG", 2, null);
            // TODO: Use Currencies.GuildGold when it's available.
            // var gg = Currencies.GuildGold;
            var gg = ncg;
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            var validatorPublicKey = new PrivateKey().PublicKey;
            world = world.MintAsset(context, validatorPublicKey.Address, gg * 100);
            var promoteFAV = gg * 10;
            world = new PromoteValidator(validatorPublicKey, promoteFAV).Execute(new ActionContext
            {
                PreviousState = world,
                Signer = validatorPublicKey.Address,
            });

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SetValidatorCommission(validatorPublicKey.Address, 12).Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = validatorPublicKey.Address,
                }));
        }
    }
}
