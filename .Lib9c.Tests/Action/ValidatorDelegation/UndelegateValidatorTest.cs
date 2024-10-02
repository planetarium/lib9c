namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Numerics;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Action.ValidatorDelegation;
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
        // Given
        var world = World;
        var actionContext = new ActionContext { };
        var validatorKey = new PrivateKey();
        var height = 1L;
        var validatorGold = NCG * 10;
        world = EnsureToMintAsset(world, validatorKey, NCG * 100, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);

        // When
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetValidatorDelegatee(validatorKey.Address);
        var expectedBond = expectedRepository.GetBond(expectedDelegatee, validatorKey.Address);
        var undelegateValidator = new UndelegateValidator(validatorKey.Address, expectedBond.Share);
        actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
            BlockIndex = height++,
        };

        world = undelegateValidator.Execute(actionContext);

        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualDelegatee = actualRepository.GetValidatorDelegatee(validatorKey.Address);
        var actualValidatorList = actualRepository.GetValidatorList();
        var actualBond = actualRepository.GetBond(actualDelegatee, validatorKey.Address);

        Assert.NotEqual(expectedDelegatee.Delegators, actualDelegatee.Delegators);
        Assert.NotEqual(expectedDelegatee.Validator.Power, actualDelegatee.Validator.Power);
        Assert.Equal(BigInteger.Zero, actualDelegatee.Validator.Power);
        Assert.Empty(actualValidatorList.Validators);
        Assert.NotEqual(expectedBond.Share, actualBond.Share);
        Assert.Equal(BigInteger.Zero, actualBond.Share);
    }

    [Fact]
    public void Execute_FromInvalidValidtor_Throw()
    {
        // Given
        var world = World;
        var delegatorKey = new PrivateKey();
        var validatorKey = new PrivateKey();
        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey, NCG * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey, NCG * 10, height++);
        world = EnsureToMintAsset(world, delegatorKey, NCG * 10, height++);
        world = EnsureBondedDelegator(world, delegatorKey, validatorKey, NCG * 10, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
            BlockIndex = height++,
        };
        var undelegateValidator = new UndelegateValidator(new PrivateKey().Address, 10);

        // Then
        Assert.Throws<FailedLoadStateException>(
            () => undelegateValidator.Execute(actionContext));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Execute_WithNotPositiveShare_Throw(long share)
    {
        // Given
        var world = World;
        var delegatorKey = new PrivateKey();
        var validatorKey = new PrivateKey();
        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey, NCG * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey, NCG * 10, height++);
        world = EnsureToMintAsset(world, delegatorKey, NCG * 10, height++);
        world = EnsureBondedDelegator(world, delegatorKey, validatorKey, NCG * 10, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
            BlockIndex = height++,
        };
        var undelegateValidator = new UndelegateValidator(validatorKey.Address, share);

        // Then
        Assert.Throws<ArgumentOutOfRangeException>(
            () => undelegateValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_WithoutDelegating_Throw()
    {
        // Given
        var world = World;
        var delegatorKey = new PrivateKey();
        var validatorKey = new PrivateKey();
        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey, NCG * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey, NCG * 10, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
            BlockIndex = height++,
        };
        var undelegateValidator = new UndelegateValidator(
            validatorKey.Address, 10);

        // Then
        Assert.Throws<ArgumentOutOfRangeException>(
            () => undelegateValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_FromJailedValidator()
    {
        // Given
        var world = World;
        var delegatorKey = new PrivateKey();
        var validatorKey = new PrivateKey();
        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey, NCG * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey, NCG * 10, height++);
        world = EnsureToMintAsset(world, delegatorKey, NCG * 10, height++);
        world = EnsureBondedDelegator(world, delegatorKey, validatorKey, NCG * 10, height++);
        world = EnsureJailedValidator(world, validatorKey, ref height);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
            BlockIndex = height,
        };
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetValidatorDelegatee(validatorKey.Address);
        var expectedBond = expectedRepository.GetBond(expectedDelegatee, delegatorKey.Address);

        var undelegateValidator = new UndelegateValidator(validatorKey.Address, 10);
        world = undelegateValidator.Execute(actionContext);

        // Then
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualDelegatee = actualRepository.GetValidatorDelegatee(validatorKey.Address);
        var actualBond = actualRepository.GetBond(actualDelegatee, delegatorKey.Address);

        Assert.Equal(expectedBond.Share - 10, actualBond.Share);
    }

    [Fact]
    public void Execute_FromTombstonedValidator()
    {
        // Given
        var world = World;
        var delegatorKey = new PrivateKey();
        var validatorKey = new PrivateKey();
        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey, NCG * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey, NCG * 10, height++);
        world = EnsureToMintAsset(world, delegatorKey, NCG * 10, height++);
        world = EnsureBondedDelegator(world, delegatorKey, validatorKey, NCG * 10, height++);
        world = EnsureTombstonedValidator(world, validatorKey, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
            BlockIndex = height,
        };
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetValidatorDelegatee(validatorKey.Address);
        var expectedBond = expectedRepository.GetBond(expectedDelegatee, delegatorKey.Address);

        var undelegateValidator = new UndelegateValidator(validatorKey.Address, 10);
        world = undelegateValidator.Execute(actionContext);

        // Then
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualDelegatee = actualRepository.GetValidatorDelegatee(validatorKey.Address);
        var actualBond = actualRepository.GetBond(actualDelegatee, delegatorKey.Address);

        Assert.Equal(expectedBond.Share - 10, actualBond.Share);
    }

    [Fact]
    public void Execute_CannotBeJailedDueToDelegatorUndelegating()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var delegatorKey = new PrivateKey();
        var validatorGold = NCG * 10;
        var delegatorGold = NCG * 10;
        var actionContext = new ActionContext { };

        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
        world = EnsureToMintAsset(world, delegatorKey, delegatorGold, height++);
        world = EnsureBondedDelegator(world, delegatorKey, validatorKey, delegatorGold, height++);
        world = EnsureUnbondingDelegator(world, validatorKey, validatorKey, 10, height++);
        world = EnsureUnjailedValidator(world, validatorKey, ref height);

        // When
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetValidatorDelegatee(validatorKey.Address);
        var expectedJailed = expectedDelegatee.Jailed;

        var undelegateValidator = new UndelegateValidator(validatorKey.Address, 10);
        actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
            BlockIndex = height,
        };
        world = undelegateValidator.Execute(actionContext);

        // Then
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualDelegatee = actualRepository.GetValidatorDelegatee(validatorKey.Address);
        var actualJailed = actualDelegatee.Jailed;

        Assert.False(actualJailed);
        Assert.Equal(expectedJailed, actualJailed);
    }
}
