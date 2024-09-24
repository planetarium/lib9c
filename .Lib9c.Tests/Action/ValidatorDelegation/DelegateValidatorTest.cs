namespace Lib9c.Tests.Action.ValidatorDelegation
{
    using System;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.ValidatorDelegation;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.ValidatorDelegation;
    using Xunit;

    public class DelegateValidatorTest : ValidatorDelegationTestBase
    {
        [Fact]
        public void Serialization()
        {
            var address = new PrivateKey().Address;
            var gg = Currencies.GuildGold;
            var fav = gg * 10;
            var action = new DelegateValidator(address, fav);
            var plainValue = action.PlainValue;

            var deserialized = new DelegateValidator();
            deserialized.LoadPlainValue(plainValue);
            Assert.Equal(address, deserialized.ValidatorDelegatee);
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

            var validatorPublicKey = new PrivateKey().PublicKey;
            world = world.MintAsset(context, validatorPublicKey.Address, gg * 100);
            var promoteFAV = gg * 10;
            world = new PromoteValidator(validatorPublicKey, promoteFAV).Execute(new ActionContext
            {
                PreviousState = world,
                Signer = validatorPublicKey.Address,
            });

            var publicKey = new PrivateKey().PublicKey;
            world = world.MintAsset(context, publicKey.Address, gg * 100);
            var delegateFAV = gg * 20;
            var action = new DelegateValidator(validatorPublicKey.Address, delegateFAV);
            world = action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = publicKey.Address,
            });

            var repository = new ValidatorRepository(world, context);
            var validator = repository.GetValidatorDelegatee(validatorPublicKey.Address);
            var bond = repository.GetBond(validator, publicKey.Address);
            var validatorList = repository.GetValidatorList();

            Assert.Contains(publicKey.Address, validator.Delegators);
            Assert.Equal(delegateFAV.RawValue, bond.Share);
            Assert.Equal(promoteFAV.RawValue + delegateFAV.RawValue, validator.Validator.Power);
            Assert.Equal(validator.Validator, Assert.Single(validatorList.Validators));
            Assert.Equal(gg * 80, world.GetBalance(publicKey.Address, gg));
        }

        [Fact]
        public void CannotDelegateWithInvalidCurrency()
        {
            IWorld world = new World(MockUtil.MockModernWorldState);
            var context = new ActionContext { };
            var ncg = Currency.Uncapped("NCG", 2, null);
            // TODO: Use Currencies.GuildGold when it's available.
            // var gg = Currencies.GuildGold;
            var gg = ncg;
            var invalid = Currency.Uncapped("invalid", 2, null);
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

            var publicKey = new PrivateKey().PublicKey;
            world = world.MintAsset(context, publicKey.Address, invalid * 100);
            var delegateFAV = invalid * 20;
            var action = new DelegateValidator(validatorPublicKey.Address, delegateFAV);
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = publicKey.Address,
            }));
        }

        [Fact]
        public void CannotDelegateWithInsufficientBalance()
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

            var publicKey = new PrivateKey().PublicKey;
            var delegateFAV = gg * 20;
            var action = new DelegateValidator(validatorPublicKey.Address, delegateFAV);
            Assert.Throws<InsufficientBalanceException>(() => action.Execute(new ActionContext
            {
                PreviousState = world,
                Signer = publicKey.Address,
            }));
        }

        [Fact]
        public void CannotDelegateToInvalidValidator()
        {
            // Given
            var world = World;
            var validatorPrivateKey = new PrivateKey();
            var delegatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            world = MintAsset(world, delegatorPrivateKey, NCG * 100, blockHeight++);

            // When
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = delegatorPrivateKey.Address,
            };
            var delegateValidator = new DelegateValidator(validatorPrivateKey.Address, NCG * 10);

            // Then
            Assert.Throws<FailedLoadStateException>(
                () => delegateValidator.Execute(actionContext));
        }
    }
}
