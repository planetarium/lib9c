#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System.Numerics;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class ReleaseValidatorUnbondingsTest : ValidatorDelegationTestBase
{
    [Fact]
    public void Serialization()
    {
        var action = new ReleaseValidatorUnbondings();
        var plainValue = action.PlainValue;

        var deserialized = new ReleaseValidatorUnbondings();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var validatorGold = DelegationCurrency * 50;
        var validatorBalance = DelegationCurrency * 100;
        var share = new BigInteger(10);
        var height = 1L;
        var actionContext = new ActionContext { };

        world = EnsureToMintAsset(world, validatorKey, validatorBalance, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
        world = EnsureUnbondingValidator(world, validatorKey.Address, share, height);

        // When
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetValidatorDelegatee(validatorKey.Address);
        var expectedUnbondingSet = expectedRepository.GetUnbondingSet();
        var expectedReleaseCount = expectedUnbondingSet.UnbondingRefs.Count;
        var expectedDepositGold = expectedDelegatee.FAVFromShare(share);
        var expectedBalance = world.GetBalance(validatorKey.Address, DelegationCurrency) + expectedDepositGold;

        var releaseValidatorUnbondings = new ReleaseValidatorUnbondings(validatorKey.Address);
        actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
            BlockIndex = height + ValidatorDelegatee.ValidatorUnbondingPeriod,
        };
        world = releaseValidatorUnbondings.Execute(actionContext);

        // Then
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualBalance = world.GetBalance(validatorKey.Address, DelegationCurrency);
        var actualUnbondingSet = actualRepository.GetUnbondingSet();
        var actualReleaseCount = actualUnbondingSet.UnbondingRefs.Count;

        Assert.Equal(expectedBalance, actualBalance);
        Assert.NotEqual(expectedUnbondingSet.IsEmpty, actualUnbondingSet.IsEmpty);
        Assert.True(actualUnbondingSet.IsEmpty);
        Assert.Equal(expectedReleaseCount - 1, actualReleaseCount);
    }

    [Fact]
    public void Execute_ThereIsNoUnbonding_AtEarlyHeight()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        var actionContext = new ActionContext { };
        var share = new BigInteger(10);

        world = EnsureToMintAsset(world, validatorKey, DelegationCurrency * 100, height++);
        world = EnsurePromotedValidator(world, validatorKey, DelegationCurrency * 50, height++);
        world = EnsureUnbondingValidator(world, validatorKey.Address, share, height);

        // When
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedUnbondingSet = expectedRepository.GetUnbondingSet();
        var expectedReleaseCount = expectedUnbondingSet.UnbondingRefs.Count;

        var releaseValidatorUnbondings = new ReleaseValidatorUnbondings(validatorKey.Address);
        actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
            BlockIndex = height,
        };
        world = releaseValidatorUnbondings.Execute(actionContext);

        // Then
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualUnbondingSet = actualRepository.GetUnbondingSet();
        var actualReleaseCount = actualUnbondingSet.UnbondingRefs.Count;

        Assert.Equal(expectedUnbondingSet.IsEmpty, actualUnbondingSet.IsEmpty);
        Assert.False(actualUnbondingSet.IsEmpty);
        Assert.Equal(expectedReleaseCount, actualReleaseCount);
    }
}
