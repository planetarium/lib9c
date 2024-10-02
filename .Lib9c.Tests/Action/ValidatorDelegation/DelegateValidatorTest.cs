namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class DelegateValidatorTest : ValidatorDelegationTestBase
{
    [Fact]
    public void Serialization()
    {
        var address = new PrivateKey().Address;
        var gold = NCG * 10;
        var action = new DelegateValidator(address, gold);
        var plainValue = action.PlainValue;

        var deserialized = new DelegateValidator();
        deserialized.LoadPlainValue(plainValue);
        Assert.Equal(address, deserialized.ValidatorDelegatee);
        Assert.Equal(gold, deserialized.FAV);
    }

    [Fact]
    public void Execute()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var delegatorKey = new PrivateKey();
        var height = 1L;
        var validatorGold = NCG * 10;
        var delegatorGold = NCG * 20;
        world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
        world = EnsureToMintAsset(world, delegatorKey, NCG * 100, height++);

        // When
        var delegateValidator = new DelegateValidator(validatorKey.Address, delegatorGold);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
        };
        world = delegateValidator.Execute(actionContext);

        // Then
        var repository = new ValidatorRepository(world, actionContext);
        var validator = repository.GetValidatorDelegatee(validatorKey.Address);
        var bond = repository.GetBond(validator, delegatorKey.Address);
        var validatorList = repository.GetValidatorList();

        Assert.Contains(delegatorKey.Address, validator.Delegators);
        Assert.Equal(delegatorGold.RawValue, bond.Share);
        Assert.Equal(validatorGold.RawValue + delegatorGold.RawValue, validator.Validator.Power);
        Assert.Equal(validator.Validator, Assert.Single(validatorList.Validators));
        Assert.Equal(NCG * 80, world.GetBalance(delegatorKey.Address, NCG));
    }

    [Fact]
    public void Execute_WithInvalidCurrency_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var delegatorKey = new PrivateKey();
        var height = 1L;
        var validatorGold = NCG * 10;
        var delegatorDollar = Dollar * 20;
        world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
        world = EnsureToMintAsset(world, delegatorKey, delegatorDollar, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
            BlockIndex = height++,
        };
        var delegateValidator = new DelegateValidator(validatorKey.Address, delegatorDollar);

        // Then
        Assert.Throws<InvalidOperationException>(() => delegateValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_WithInsufficientBalance_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var delegatorKey = new PrivateKey();
        var validatorGold = NCG * 10;
        var delegatorGold = NCG * 10;
        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
        world = EnsureToMintAsset(world, delegatorKey, delegatorGold, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
            BlockIndex = height++,
        };
        var delegateValidator = new DelegateValidator(validatorKey.Address, NCG * 11);

        // Then
        Assert.Throws<InsufficientBalanceException>(() => delegateValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_ToInvalidValidator_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var delegatorKey = new PrivateKey();
        var height = 1L;
        world = EnsureToMintAsset(world, delegatorKey, NCG * 100, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
        };
        var delegateValidator = new DelegateValidator(validatorKey.Address, NCG * 10);

        // Then
        Assert.Throws<FailedLoadStateException>(() => delegateValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_ToTombstonedValidator_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var delegatorKey = new PrivateKey();
        var height = 1L;
        var validatorGold = NCG * 10;
        var delegatorGold = NCG * 10;
        world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
        world = EnsureTombstonedValidator(world, validatorKey, height++);
        world = EnsureToMintAsset(world, delegatorKey, delegatorGold, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
        };
        var delegateValidator = new DelegateValidator(validatorKey.Address, delegatorGold);

        // Then
        Assert.Throws<InvalidOperationException>(() => delegateValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_CannotBeJailedDueToDelegatorDelegating()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var delegatorKey = new PrivateKey();
        var validatorGold = NCG * 10;
        var delegatorGold = NCG * 10;
        var delegatorBalance = NCG * 100;
        var actionContext = new ActionContext { };

        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
        world = EnsureToMintAsset(world, delegatorKey, delegatorBalance, height++);
        world = EnsureBondedDelegator(world, delegatorKey, validatorKey, delegatorGold, height++);
        world = EnsureUnbondingDelegator(world, validatorKey, validatorKey, 10, height++);
        world = EnsureUnjailedValidator(world, validatorKey, ref height);

        // When
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetValidatorDelegatee(validatorKey.Address);
        var expectedJailed = expectedDelegatee.Jailed;

        var delegateValidator = new DelegateValidator(validatorKey.Address, 1 * NCG);
        actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
            BlockIndex = height,
        };
        world = delegateValidator.Execute(actionContext);

        // Then
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualDelegatee = actualRepository.GetValidatorDelegatee(validatorKey.Address);
        var actualJailed = actualDelegatee.Jailed;

        Assert.False(actualJailed);
        Assert.Equal(expectedJailed, actualJailed);
    }
}
