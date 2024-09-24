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

    public class UndelegateValidatorTest : ValidatorDelegationTestBase
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

        [Fact]
        public void Undelegate_FromInvalidValidtor_Throw()
        {
            // Given
            var world = World;
            var delegatorPrivateKey = new PrivateKey();
            var validatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, NCG * 10, blockHeight++);
            world = EnsureDelegatorToBeBond(
                world, delegatorPrivateKey, validatorPrivateKey, NCG * 10, blockHeight++);

            // When
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = delegatorPrivateKey.Address,
                BlockIndex = blockHeight++,
            };
            var undelegateValidator = new UndelegateValidator(
                new PrivateKey().Address, 10);

            // Then
            Assert.Throws<FailedLoadStateException>(
                () => undelegateValidator.Execute(actionContext));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Undelegate_NotPositiveShare_Throw(long share)
        {
            // Given
            var world = World;
            var delegatorPrivateKey = new PrivateKey();
            var validatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, NCG * 10, blockHeight++);
            world = EnsureDelegatorToBeBond(
                world, delegatorPrivateKey, validatorPrivateKey, NCG * 10, blockHeight++);

            // When
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = delegatorPrivateKey.Address,
                BlockIndex = blockHeight++,
            };
            var undelegateValidator = new UndelegateValidator(
                validatorPrivateKey.Address, share);

            // Then
            Assert.Throws<ArgumentOutOfRangeException>(
                () => undelegateValidator.Execute(actionContext));
        }

        [Fact]
        public void Undelegate_NotDelegated_Throw()
        {
            // Given
            var world = World;
            var delegatorPrivateKey = new PrivateKey();
            var validatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, NCG * 10, blockHeight++);

            // When
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = delegatorPrivateKey.Address,
                BlockIndex = blockHeight++,
            };
            var undelegateValidator = new UndelegateValidator(
                validatorPrivateKey.Address, 10);

            // Then
            Assert.Throws<ArgumentOutOfRangeException>(
                () => undelegateValidator.Execute(actionContext));
        }

        [Fact]
        public void Undelegate_FromJailedValidator()
        {
            // Given
            var world = World;
            var delegatorPrivateKey = new PrivateKey();
            var validatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, NCG * 10, blockHeight++);
            world = EnsureDelegatorToBeBond(
                world, delegatorPrivateKey, validatorPrivateKey, NCG * 10, blockHeight++);
            world = EnsureValidatorToBeJailed(
                world, validatorPrivateKey, ref blockHeight);

            // When
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = delegatorPrivateKey.Address,
                BlockIndex = blockHeight,
            };
            var expectedRepository = new ValidatorRepository(world, actionContext);
            var expectedDelegatee = expectedRepository.GetValidatorDelegatee(
                validatorPrivateKey.PublicKey.Address);
            var expectedBond = expectedRepository.GetBond(
                expectedDelegatee, delegatorPrivateKey.Address);

            var undelegateValidator = new UndelegateValidator(
                validatorPrivateKey.Address, 10);
            world = undelegateValidator.Execute(actionContext);

            // Then
            var actualRepository = new ValidatorRepository(world, actionContext);
            var actualDelegatee = actualRepository.GetValidatorDelegatee(
                validatorPrivateKey.PublicKey.Address);
            var actualBond = actualRepository.GetBond(actualDelegatee, delegatorPrivateKey.Address);

            Assert.Equal(expectedBond.Share - 10, actualBond.Share);
        }
    }
}
