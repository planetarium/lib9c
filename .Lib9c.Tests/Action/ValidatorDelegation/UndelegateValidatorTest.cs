namespace Lib9c.Tests.Action.ValidatorDelegation
{
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

    public class UndelegateValidatorTest
    {
        [Fact]
        public void Serialization()
        {
            var address = new PrivateKey().Address;
            var share = BigInteger.One;
            var action = new UndelegateValidator(address, share);
            var plainValue = action.PlainValue;

            var deserialized = new UndelegateValidator();
            deserialized.LoadPlainValue(plainValue);
            Assert.Equal(address, deserialized.ValidatorDelegatee);
            Assert.Equal(share, deserialized.Share);
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
            var promoteFAV = gg * 10;
            world = new PromoteValidator(publicKey, promoteFAV).Execute(new ActionContext
            {
                PreviousState = world,
                Signer = publicKey.Address,
            });

            var repository = new ValidatorRepository(world, context);
            var validator = repository.GetValidatorDelegatee(publicKey.Address);
            var bond = repository.GetBond(validator, publicKey.Address);
            var action = new UndelegateValidator(publicKey.Address, bond.Share);
            context = new ActionContext
            {
                PreviousState = world,
                Signer = publicKey.Address,
                BlockIndex = 1,
            };

            world = action.Execute(context);

            repository.UpdateWorld(world);
            validator = repository.GetValidatorDelegatee(publicKey.Address);
            var validatorList = repository.GetValidatorList();

            Assert.Empty(validator.Delegators);
            Assert.Equal(BigInteger.Zero, validator.Validator.Power);
            Assert.Empty(validatorList.Validators);

            world = new ReleaseValidatorUnbondings().Execute(new ActionContext
            {
                PreviousState = world,
                Signer = publicKey.Address,
                BlockIndex = 1,
            });

            Assert.Equal(gg * 100, world.GetBalance(publicKey.Address, gg));
        }
    }
}
