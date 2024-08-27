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
    using Nekoyume.Module.Delegation;
    using Nekoyume.Module.ValidatorDelegation;
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
            var gg = Currencies.GuildGold;
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

            var validator = world.GetValidatorDelegatee(publicKey.Address);
            var bond = world.GetBond(validator, publicKey.Address);
            var action = new UndelegateValidator(publicKey.Address, bond.Share);
            context = new ActionContext
            {
                PreviousState = world,
                Signer = publicKey.Address,
                BlockIndex = 1,
            };

            world = action.Execute(context);

            validator = world.GetValidatorDelegatee(publicKey.Address);
            var validatorList = world.GetValidatorList();

            Assert.Empty(validator.Delegators);
            Assert.Equal(BigInteger.Zero, validator.Validator.Power);
            Assert.Empty(validatorList.Validators);

            context = new ActionContext
            {
                PreviousState = world,
                Signer = publicKey.Address,
                BlockIndex = 1,
            };

            world = world.ReleaseUnbondings(context);
            Assert.Equal(gg * 100, world.GetBalance(publicKey.Address, gg));
        }
    }
}
