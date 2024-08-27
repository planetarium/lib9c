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

    public class RedelegateValidatorTest
    {
        [Fact]
        public void Serialization()
        {
            var srcAddress = new PrivateKey().Address;
            var dstAddress = new PrivateKey().Address;
            var share = BigInteger.One;
            var action = new RedelegateValidator(srcAddress, dstAddress, share);
            var plainValue = action.PlainValue;

            var deserialized = new RedelegateValidator();
            deserialized.LoadPlainValue(plainValue);
            Assert.Equal(srcAddress, deserialized.SrcValidatorDelegatee);
            Assert.Equal(dstAddress, deserialized.DstValidatorDelegatee);
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

            var srcPublicKey = new PrivateKey().PublicKey;
            world = world.MintAsset(context, srcPublicKey.Address, gg * 100);
            var promoteFAV = gg * 10;
            world = new PromoteValidator(srcPublicKey, promoteFAV).Execute(new ActionContext
            {
                PreviousState = world,
                Signer = srcPublicKey.Address,
            });

            var dstPublicKey = new PrivateKey().PublicKey;
            world = world.MintAsset(context, dstPublicKey.Address, gg * 100);
            world = new PromoteValidator(dstPublicKey, promoteFAV).Execute(new ActionContext
            {
                PreviousState = world,
                Signer = dstPublicKey.Address,
            });

            var srcValidator = world.GetValidatorDelegatee(srcPublicKey.Address);
            var bond = world.GetBond(srcValidator, srcPublicKey.Address);
            var action = new RedelegateValidator(srcPublicKey.Address, dstPublicKey.Address, bond.Share);
            context = new ActionContext
            {
                PreviousState = world,
                Signer = srcPublicKey.Address,
                BlockIndex = 1,
            };

            world = action.Execute(context);

            srcValidator = world.GetValidatorDelegatee(srcPublicKey.Address);
            var dstValidator = world.GetValidatorDelegatee(dstPublicKey.Address);
            var validatorList = world.GetValidatorList();
            var dstBond = world.GetBond(dstValidator, srcPublicKey.Address);

            Assert.Contains(srcPublicKey.Address, dstValidator.Delegators);
            Assert.Equal(dstValidator.Validator, Assert.Single(validatorList.Validators));
            Assert.Equal((gg * 10).RawValue, dstBond.Share);
            Assert.Equal(gg * 20, dstValidator.TotalDelegated);
            Assert.Equal((gg * 20).RawValue, dstValidator.TotalShares);
            Assert.Equal((gg * 20).RawValue, dstValidator.Power);
        }
    }
}
