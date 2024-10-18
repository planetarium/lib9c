#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Linq;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.Model.State;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class UpdateValidatorsTest : ValidatorDelegationTestBase
{
    [Fact]
    public void Serialization()
    {
        var action = new UpdateValidators();
        var plainValue = action.PlainValue;

        var deserialized = new UpdateValidators();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        // Given
        const int length = 10;
        var world = World;
        var validatorKeys = CreateArray(length, _ => new PrivateKey());
        var validatorGolds = CreateArray(length, i => DelegationCurrency * Random.Shared.Next(1, length + 1));
        var height = 1L;
        var actionContext = new ActionContext { };
        world = EnsureToMintAssets(world, validatorKeys, validatorGolds, height++);
        world = EnsurePromotedValidators(world, validatorKeys, validatorGolds, height);

        // When
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedValidators = expectedRepository.GetValidatorList()
            .GetBonded().OrderBy(item => item.OperatorAddress).ToList();
        actionContext = new ActionContext
        {
            BlockIndex = height,
            PreviousState = world,
            Signer = AdminKey.Address,
        };
        world = new UpdateValidators().Execute(actionContext);

        // Then
        var actualValidators = world.GetValidatorSet().Validators;
        Assert.Equal(expectedValidators.Count, actualValidators.Count);
        for (var i = 0; i < expectedValidators.Count; i++)
        {
            var expectedValidator = expectedValidators[i];
            var actualValidator = actualValidators[i];
            Assert.Equal(expectedValidator, actualValidator);
        }
    }

    [Fact]
    public void Execute_ExcludesTombstonedValidator()
    {
        // Given
        const int length = 10;
        var world = World;
        var validatorKeys = CreateArray(length, _ => new PrivateKey());
        var validatorGolds = CreateArray(length, i => DelegationCurrency * 100);
        var height = 1L;
        var validatorGold = DelegationCurrency * 100;
        var actionContext = new ActionContext { };
        world = EnsureToMintAssets(world, validatorKeys, validatorGolds, height++);
        world = EnsurePromotedValidators(world, validatorKeys, validatorGolds, height++);

        // When
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedValidators = expectedRepository.GetValidatorList()
            .GetBonded().OrderBy(item => item.OperatorAddress).ToList();
        var updateValidators = new UpdateValidators();
        world = EnsureTombstonedValidator(world, validatorKeys[0], height);
        actionContext = new ActionContext
        {
            BlockIndex = height,
            PreviousState = world,
            Signer = AdminKey.Address,
        };
        world = updateValidators.Execute(actionContext);

        // Then
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualValidators = actualRepository.GetValidatorList()
            .GetBonded().OrderBy(item => item.OperatorAddress).ToList();
        var tombstonedValidator = actualRepository.GetValidatorDelegatee(validatorKeys[0].Address);

        Assert.True(tombstonedValidator.Tombstoned);
        Assert.Equal(expectedValidators.Count - 1, actualValidators.Count);
        Assert.Contains(expectedValidators, v => v.PublicKey == validatorKeys[0].PublicKey);
        Assert.DoesNotContain(actualValidators, v => v.PublicKey == validatorKeys[0].PublicKey);
    }
}
