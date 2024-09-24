namespace Lib9c.Tests.Action.ValidatorDelegation
{
    using System;
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

    public class PromoteValidatorTest : ValidatorDelegationTestBase
    {
        [Fact]
        public void Serialization()
        {
            var publicKey = new PrivateKey().PublicKey;
            var gg = Currencies.GuildGold;
            var fav = gg * 10;
            var action = new PromoteValidator(publicKey, fav);
            var plainValue = action.PlainValue;

            var deserialized = new PromoteValidator();
            deserialized.LoadPlainValue(plainValue);
            Assert.Equal(publicKey, deserialized.PublicKey);
            Assert.Equal(fav, deserialized.FAV);
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

            var publicKey = new PrivateKey().PublicKey;
            world = world.MintAsset(context, publicKey.Address, gg * 100);
            var fav = gg * 10;
            var action = new PromoteValidator(publicKey, fav);

            world = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = publicKey.Address,
            });

            var repository = new ValidatorRepository(world, context);
            var validator = repository.GetValidatorDelegatee(publicKey.Address);
            var bond = repository.GetBond(validator, publicKey.Address);
            var validatorList = repository.GetValidatorList();

            Assert.Equal(publicKey.Address, Assert.Single(validator.Delegators));
            Assert.Equal(fav.RawValue, bond.Share);
            Assert.Equal(validator.Validator, Assert.Single(validatorList.Validators));
            Assert.Equal(validator.Validator, Assert.Single(validatorList.GetBonded()));
            Assert.Equal(gg * 90, world.GetBalance(publicKey.Address, gg));
            Assert.Empty(validatorList.GetUnbonded());
        }

        [Fact]
        public void CannotPromoteWithInvalidPublicKey()
        {
            IWorld world = new World(MockUtil.MockModernWorldState);
            var context = new ActionContext { };
            var ncg = Currency.Uncapped("NCG", 2, null);
            var gg = Currencies.GuildGold;
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            var publicKey = new PrivateKey().PublicKey;
            world = world.MintAsset(context, publicKey.Address, gg * 100);
            var fav = gg * 10;
            var action = new PromoteValidator(new PrivateKey().PublicKey, fav);

            Assert.Throws<ArgumentException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = publicKey.Address,
            }));
        }

        [Fact]
        public void CannotPromoteWithInvalidCurrency()
        {
            IWorld world = new World(MockUtil.MockModernWorldState);
            var context = new ActionContext { };
            var ncg = Currency.Uncapped("NCG", 2, null);
            var gg = Currency.Uncapped("invalid", 2, null);
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            var publicKey = new PrivateKey().PublicKey;
            world = world.MintAsset(context, publicKey.Address, gg * 100);
            var fav = gg * 10;
            var action = new PromoteValidator(publicKey, fav);

            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = publicKey.Address,
            }));
        }

        [Fact]
        public void CannotPromoteWithInsufficientBalance()
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

            var publicKey = new PrivateKey().PublicKey;
            var fav = gg * 10;
            var action = new PromoteValidator(publicKey, fav);

            Assert.Throws<InsufficientBalanceException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = publicKey.Address,
            }));
        }

        [Fact]
        public void Promote_PromotedValidator_Throw()
        {
            // Given
            var world = World;
            var validatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, NCG * 10, blockHeight++);

            // When
            var promoteValidator = new PromoteValidator(
                validatorPrivateKey.PublicKey, NCG * 10);
            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight++,
                Signer = validatorPrivateKey.Address,
            };

            // Then
            Assert.Throws<InvalidOperationException>(
                () => promoteValidator.Execute(actionContext));
        }
    }
}
