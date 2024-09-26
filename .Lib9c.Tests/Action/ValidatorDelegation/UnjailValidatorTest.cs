#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class UnjailValidatorTest : ValidatorDelegationTestBase
{
    [Fact]
    public void Serialization()
    {
        var action = new UnjailValidator();
        var plainValue = action.PlainValue;

        var deserialized = new UnjailValidator();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        world = EnsurePromotedValidator(world, validatorKey, NCG * 10, mint: true, height++);
        world = EnsureJailedValidator(world, validatorKey, ref height);

        // When
        var unjailValidator = new UnjailValidator();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height + SlashValidator.AbstainJailTime,
            Signer = validatorKey.Address,
        };
        world = unjailValidator.Execute(actionContext);

        // Then
        var repository = new ValidatorRepository(world, actionContext);
        var delegatee = repository.GetValidatorDelegatee(validatorKey.Address);
        Assert.False(delegatee.Jailed);
        Assert.Equal(-1, delegatee.JailedUntil);
        Assert.False(delegatee.Tombstoned);
    }

    [Fact]
    public void Execute_OnNotPromotedValidator_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;

        // When
        var unjailValidator = new UnjailValidator();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height + SlashValidator.AbstainJailTime,
            Signer = validatorKey.Address,
        };

        // Then
        Assert.Throws<FailedLoadStateException>(() => unjailValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_OnNotJailedValidator_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        world = EnsurePromotedValidator(world, validatorKey, NCG * 10, mint: true, height++);

        // When
        var unjailValidator = new UnjailValidator();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height + SlashValidator.AbstainJailTime,
            Signer = validatorKey.Address,
        };

        // Then
        Assert.Throws<InvalidOperationException>(() => unjailValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_TooEarly_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        world = EnsurePromotedValidator(world, validatorKey, NCG * 10, mint: true, height++);
        world = EnsureJailedValidator(world, validatorKey, ref height);

        // When
        var unjailValidator = new UnjailValidator();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height + SlashValidator.AbstainJailTime - 1,
            Signer = validatorKey.Address,
        };

        // Then
        Assert.Throws<InvalidOperationException>(
            () => unjailValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_OnTombstonedValidator_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        world = EnsurePromotedValidator(world, validatorKey, NCG * 10, mint: true, height++);
        world = EnsureTombstonedValidator(world, validatorKey, height++);

        // When
        var unjailValidator = new UnjailValidator();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height + SlashValidator.AbstainJailTime,
            Signer = validatorKey.Address,
        };

        // Then
        Assert.Throws<InvalidOperationException>(
            () => unjailValidator.Execute(actionContext));
    }
}