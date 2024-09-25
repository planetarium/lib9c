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
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++, mint: true);
        world = MintAsset(world, delegatorKey, NCG * 100, height++);

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
        var delegatorGold = Dollar * 20;
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++, mint: true);
        world = MintAsset(world, delegatorKey, delegatorGold, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
            BlockIndex = height++,
        };
        var delegateValidator = new DelegateValidator(validatorKey.Address, delegatorGold);

        // Then
        Assert.Throws<InvalidOperationException>(
            () => delegateValidator.Execute(actionContext));
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
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++, mint: true);
        world = MintAsset(world, delegatorKey, delegatorGold, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
            BlockIndex = height++,
        };
        var delegateValidator = new DelegateValidator(validatorKey.Address, NCG * 11);

        // Then
        Assert.Throws<InsufficientBalanceException>(
            () => delegateValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_ToInvalidValidator_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var delegatorKey = new PrivateKey();
        var height = 1L;
        world = MintAsset(world, delegatorKey, NCG * 100, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
        };
        var delegateValidator = new DelegateValidator(validatorKey.Address, NCG * 10);

        // Then
        Assert.Throws<FailedLoadStateException>(
            () => delegateValidator.Execute(actionContext));
    }
}
