namespace Lib9c.Tests.Action.ValidatorDelegation
{
    using System;
    using System.Numerics;
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

    public class RedelegateValidatorTest : ValidatorDelegationTestBase
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
            // TODO: Use Currencies.GuildGold when it's available.
            // var gg = Currencies.GuildGold;
            var gg = ncg;
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

            var repository = new ValidatorRepository(world, context);
            var srcValidator = repository.GetValidatorDelegatee(srcPublicKey.Address);
            var bond = repository.GetBond(srcValidator, srcPublicKey.Address);
            var action = new RedelegateValidator(srcPublicKey.Address, dstPublicKey.Address, bond.Share);
            context = new ActionContext
            {
                PreviousState = world,
                Signer = srcPublicKey.Address,
                BlockIndex = 1,
            };

            world = action.Execute(context);

            repository.UpdateWorld(world);
            srcValidator = repository.GetValidatorDelegatee(srcPublicKey.Address);
            var dstValidator = repository.GetValidatorDelegatee(dstPublicKey.Address);
            var validatorList = repository.GetValidatorList();
            var dstBond = repository.GetBond(dstValidator, srcPublicKey.Address);

            Assert.Contains(srcPublicKey.Address, dstValidator.Delegators);
            Assert.Equal(dstValidator.Validator, Assert.Single(validatorList.Validators));
            Assert.Equal((gg * 10).RawValue, dstBond.Share);
            Assert.Equal(gg * 20, dstValidator.TotalDelegated);
            Assert.Equal((gg * 20).RawValue, dstValidator.TotalShares);
            Assert.Equal((gg * 20).RawValue, dstValidator.Power);
        }

        [Fact]
        public void Redelegate_ToInvalidValidator_Throw()
        {
            // Given
            var world = World;
            var validatorPrivateKey = new PrivateKey();
            var delegatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, NCG * 10, blockHeight++);
            world = EnsureDelegatorToBeBond(
                world, delegatorPrivateKey, validatorPrivateKey, NCG * 10, blockHeight++);

            // When
            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight++,
                Signer = delegatorPrivateKey.Address,
            };
            var redelegateValidator = new RedelegateValidator(
                validatorPrivateKey.PublicKey.Address,
                new PrivateKey().PublicKey.Address,
                10);

            // Then
            Assert.Throws<FailedLoadStateException>(
                () => redelegateValidator.Execute(actionContext));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Redelegate_NotPositiveShare_Throw(long share)
        {
            // Given
            var world = World;
            var validatorPrivateKey1 = new PrivateKey();
            var validatorPrivateKey2 = new PrivateKey();
            var delegatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey1, NCG * 10, blockHeight++);
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey2, NCG * 10, blockHeight++);
            world = EnsureDelegatorToBeBond(
                world, delegatorPrivateKey, validatorPrivateKey1, NCG * 10, blockHeight++);

            // When
            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight++,
                Signer = delegatorPrivateKey.Address,
            };
            var redelegateValidator = new RedelegateValidator(
                validatorPrivateKey1.PublicKey.Address,
                validatorPrivateKey2.PublicKey.Address,
                share);

            // Then
            Assert.Throws<ArgumentOutOfRangeException>(
                () => redelegateValidator.Execute(actionContext));
        }

        [Fact]
        public void Redelegate_OverShare_Throw()
        {
            // Given
            var world = World;
            var validatorPrivateKey1 = new PrivateKey();
            var validatorPrivateKey2 = new PrivateKey();
            var delegatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey1, NCG * 10, blockHeight++);
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey2, NCG * 10, blockHeight++);
            world = EnsureDelegatorToBeBond(
                world, delegatorPrivateKey, validatorPrivateKey1, NCG * 10, blockHeight++);

            // When
            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight++,
                Signer = delegatorPrivateKey.Address,
            };
            var repository = new ValidatorRepository(world, actionContext);
            var delegatee = repository.GetDelegatee(validatorPrivateKey1.PublicKey.Address);
            var bond = repository.GetBond(delegatee, delegatorPrivateKey.Address);
            var redelegateValidator = new RedelegateValidator(
                validatorPrivateKey1.PublicKey.Address,
                validatorPrivateKey2.PublicKey.Address,
                bond.Share + 1);

            // Then
            Assert.Throws<ArgumentOutOfRangeException>(
                () => redelegateValidator.Execute(actionContext));
        }

        [Fact]
        public void Redelegate_FromJailedValidator_Throw()
        {
            // Given
            var world = World;
            var delegatorPrivateKey = new PrivateKey();
            var validatorPrivateKey1 = new PrivateKey();
            var validatorPrivateKey2 = new PrivateKey();
            var blockHeight = 1L;
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey1, NCG * 10, blockHeight++);
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey2, NCG * 10, blockHeight++);
            world = EnsureDelegatorToBeBond(
                world, delegatorPrivateKey, validatorPrivateKey1, NCG * 10, blockHeight++);
            world = EnsureValidatorToBeJailed(
                world, validatorPrivateKey1, ref blockHeight);

            // When
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = delegatorPrivateKey.Address,
                BlockIndex = blockHeight++,
            };
            var expectedRepository = new ValidatorRepository(world, actionContext);
            var expectedDelegatee1 = expectedRepository.GetValidatorDelegatee(
                validatorPrivateKey1.PublicKey.Address);
            var expectedDelegatee2 = expectedRepository.GetValidatorDelegatee(
                validatorPrivateKey2.PublicKey.Address);
            var expectedBond1 = expectedRepository.GetBond(
                expectedDelegatee1, delegatorPrivateKey.Address);
            var expectedBond2 = expectedRepository.GetBond(
                expectedDelegatee2, delegatorPrivateKey.Address);

            var redelegateValidator = new RedelegateValidator(
                validatorPrivateKey1.Address, validatorPrivateKey2.Address, 10);
            world = redelegateValidator.Execute(actionContext);

            // Then
            var actualRepository = new ValidatorRepository(world, actionContext);
            var actualDelegatee1 = actualRepository.GetValidatorDelegatee(
                validatorPrivateKey1.PublicKey.Address);
            var actualDelegatee2 = actualRepository.GetValidatorDelegatee(
                validatorPrivateKey2.PublicKey.Address);
            var actualBond1 = actualRepository.GetBond(
                actualDelegatee1, delegatorPrivateKey.Address);
            var actualBond2 = actualRepository.GetBond(
                actualDelegatee2, delegatorPrivateKey.Address);

            Assert.Equal(expectedBond1.Share - 10, actualBond1.Share);
            Assert.Equal(expectedBond2.Share + 10, actualBond2.Share);
        }
    }
}
