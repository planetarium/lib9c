namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class PromoteValidatorTest : ValidatorDelegationTestBase
{
    [Fact]
    public void Serialization()
    {
        var publicKey = new PrivateKey().PublicKey;
        var gold = NCG * 10;
        var action = new PromoteValidator(publicKey, gold);
        var plainValue = action.PlainValue;

        var deserialized = new PromoteValidator();
        deserialized.LoadPlainValue(plainValue);
        Assert.Equal(publicKey, deserialized.PublicKey);
        Assert.Equal(gold, deserialized.FAV);
    }

    [Fact]
    public void Execute()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        var gold = NCG * 10;
        world = MintAsset(world, validatorKey, NCG * 100, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
            BlockIndex = height++,
        };
        var promoteValidator = new PromoteValidator(validatorKey.PublicKey, gold);
        world = promoteValidator.Execute(actionContext);

        // Then
        var repository = new ValidatorRepository(world, actionContext);
        var validator = repository.GetValidatorDelegatee(validatorKey.Address);
        var bond = repository.GetBond(validator, validatorKey.Address);
        var validatorList = repository.GetValidatorList();

        Assert.Equal(validatorKey.Address, Assert.Single(validator.Delegators));
        Assert.Equal(gold.RawValue, bond.Share);
        Assert.Equal(validator.Validator, Assert.Single(validatorList.Validators));
        Assert.Equal(validator.Validator, Assert.Single(validatorList.GetBonded()));
        Assert.Equal(NCG * 90, world.GetBalance(validatorKey.Address, NCG));
        Assert.Empty(validatorList.GetUnbonded());
    }

    [Fact]
    public void Execute_ToInvalidValidator_Throw()
    {
        // Given
        var world = World;
        var context = new ActionContext { };
        var validatorKey = new PrivateKey();
        var height = 1L;
        var gold = NCG * 10;
        world = MintAsset(world, validatorKey, NCG * 100, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
            BlockIndex = height++,
        };
        var promoteValidator = new PromoteValidator(new PrivateKey().PublicKey, gold);

        // Then
        Assert.Throws<ArgumentException>(
            () => promoteValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_WithInvalidCurrency_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        var gold = Dollar * 10;
        world = MintAsset(world, validatorKey, Dollar * 100, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
            BlockIndex = height++,
        };
        var promoteValidator = new PromoteValidator(validatorKey.PublicKey, gold);

        // Then
        Assert.Throws<InvalidOperationException>(
            () => promoteValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_WithInsufficientBalance_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey().PublicKey;
        var height = 1L;
        var gold = NCG * 10;

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
            BlockIndex = height++,
        };
        var promoteValidator = new PromoteValidator(validatorKey, gold);

        // Then
        Assert.Throws<InsufficientBalanceException>(
            () => promoteValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_PromotedValidator_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        world = EnsurePromotedValidator(world, validatorKey, NCG * 10, mint: true, height++);

        // When
        var promoteValidator = new PromoteValidator(validatorKey.PublicKey, NCG * 10);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Signer = validatorKey.Address,
        };

        // Then
        Assert.Throws<InvalidOperationException>(
            () => promoteValidator.Execute(actionContext));
    }
}