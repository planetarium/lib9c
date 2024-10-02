namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Numerics;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Action.ValidatorDelegation;
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
        // Given
        var world = World;
        var actionContext = new ActionContext { };
        var srcPrivateKey = new PrivateKey();
        var dstPrivateKey = new PrivateKey();
        var height = 1L;
        world = EnsureToMintAsset(world, srcPrivateKey, NCG * 10, height++);
        world = EnsurePromotedValidator(world, srcPrivateKey, NCG * 10, height++);
        world = EnsureToMintAsset(world, dstPrivateKey, NCG * 10, height++);
        world = EnsurePromotedValidator(world, dstPrivateKey, NCG * 10, height++);

        // When
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedSrcValidator = expectedRepository.GetValidatorDelegatee(srcPrivateKey.Address);
        var expectedBond = expectedRepository.GetBond(expectedSrcValidator, srcPrivateKey.Address);
        var redelegateValidator = new RedelegateValidator(
            srcPrivateKey.Address, dstPrivateKey.Address, expectedBond.Share);
        actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = srcPrivateKey.Address,
            BlockIndex = height++,
        };

        world = redelegateValidator.Execute(actionContext);

        // Then
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualDstValidator = actualRepository.GetValidatorDelegatee(dstPrivateKey.Address);
        var actualValidatorList = actualRepository.GetValidatorList();
        var actualDstBond = actualRepository.GetBond(actualDstValidator, srcPrivateKey.Address);

        Assert.Contains(srcPrivateKey.Address, actualDstValidator.Delegators);
        Assert.Single(actualValidatorList.Validators);
        Assert.Equal(actualDstValidator.Validator, actualValidatorList.Validators[0]);
        Assert.Equal((NCG * 10).RawValue, actualDstBond.Share);
        Assert.Equal(NCG * 20, actualDstValidator.TotalDelegated);
        Assert.Equal((NCG * 20).RawValue, actualDstValidator.TotalShares);
        Assert.Equal((NCG * 20).RawValue, actualDstValidator.Power);
    }

    [Fact]
    public void Execute_ToInvalidValidator_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var delegatorKey = new PrivateKey();
        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey, NCG * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey, NCG * 10, height++);
        world = EnsureToMintAsset(world, delegatorKey, NCG * 10, height++);
        world = EnsureBondedDelegator(world, delegatorKey, validatorKey, NCG * 10, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Signer = delegatorKey.Address,
        };
        var invalidAddress = new PrivateKey().Address;
        var redelegateValidator = new RedelegateValidator(validatorKey.Address, invalidAddress, 10);

        // Then
        Assert.Throws<FailedLoadStateException>(
            () => redelegateValidator.Execute(actionContext));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Execute_WithNotPositiveShare_Throw(long share)
    {
        // Given
        var world = World;
        var validatorKey1 = new PrivateKey();
        var validatorKey2 = new PrivateKey();
        var delegatorKey = new PrivateKey();
        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey1, NCG * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey1, NCG * 10, height++);
        world = EnsureToMintAsset(world, validatorKey2, NCG * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey2, NCG * 10, height++);
        world = EnsureToMintAsset(world, delegatorKey, NCG * 10, height++);
        world = EnsureBondedDelegator(world, delegatorKey, validatorKey1, NCG * 10, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Signer = delegatorKey.Address,
        };
        var redelegateValidator = new RedelegateValidator(
            validatorKey1.Address, validatorKey2.Address, share);

        // Then
        Assert.Throws<ArgumentOutOfRangeException>(
            () => redelegateValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_WithOverShare_Throw()
    {
        // Given
        var world = World;
        var validatorKey1 = new PrivateKey();
        var validatorKey2 = new PrivateKey();
        var delegatorKey = new PrivateKey();
        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey1, NCG * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey1, NCG * 10, height++);
        world = EnsureToMintAsset(world, validatorKey2, NCG * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey2, NCG * 10, height++);
        world = EnsureToMintAsset(world, delegatorKey, NCG * 10, height++);
        world = EnsureBondedDelegator(world, delegatorKey, validatorKey1, NCG * 10, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Signer = delegatorKey.Address,
        };
        var repository = new ValidatorRepository(world, actionContext);
        var delegatee = repository.GetDelegatee(validatorKey1.Address);
        var bond = repository.GetBond(delegatee, delegatorKey.Address);
        var redelegateValidator = new RedelegateValidator(
            validatorKey1.Address, validatorKey2.Address, bond.Share + 1);

        // Then
        Assert.Throws<ArgumentOutOfRangeException>(
            () => redelegateValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_ToJailedValidator()
    {
        // Given
        var world = World;
        var delegatorKey = new PrivateKey();
        var validatorKey1 = new PrivateKey();
        var validatorKey2 = new PrivateKey();
        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey1, NCG * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey1, NCG * 10, height++);
        world = EnsureToMintAsset(world, validatorKey2, NCG * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey2, NCG * 10, height++);
        world = EnsureToMintAsset(world, delegatorKey, NCG * 10, height++);
        world = EnsureBondedDelegator(world, delegatorKey, validatorKey1, NCG * 10, height++);
        world = EnsureJailedValidator(world, validatorKey1, ref height);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
            BlockIndex = height++,
        };
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee1 = expectedRepository.GetValidatorDelegatee(validatorKey1.Address);
        var expectedDelegatee2 = expectedRepository.GetValidatorDelegatee(validatorKey2.Address);
        var expectedBond1 = expectedRepository.GetBond(expectedDelegatee1, delegatorKey.Address);
        var expectedBond2 = expectedRepository.GetBond(expectedDelegatee2, delegatorKey.Address);

        var redelegateValidator = new RedelegateValidator(
            validatorKey1.Address, validatorKey2.Address, 10);
        world = redelegateValidator.Execute(actionContext);

        // Then
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualDelegatee1 = actualRepository.GetValidatorDelegatee(validatorKey1.Address);
        var actualDelegatee2 = actualRepository.GetValidatorDelegatee(validatorKey2.Address);
        var actualBond1 = actualRepository.GetBond(actualDelegatee1, delegatorKey.Address);
        var actualBond2 = actualRepository.GetBond(actualDelegatee2, delegatorKey.Address);

        Assert.Equal(expectedBond1.Share - 10, actualBond1.Share);
        Assert.Equal(expectedBond2.Share + 10, actualBond2.Share);
    }

    [Fact]
    public void Execute_ToTombstonedValidator_Throw()
    {
        // Given
        var world = World;
        var actionContext = new ActionContext { };
        var srcPrivateKey = new PrivateKey();
        var dstPrivateKey = new PrivateKey();
        var height = 1L;
        world = EnsureToMintAsset(world, srcPrivateKey, NCG * 10, height++);
        world = EnsurePromotedValidator(world, srcPrivateKey, NCG * 10, height++);
        world = EnsureToMintAsset(world, dstPrivateKey, NCG * 10, height++);
        world = EnsurePromotedValidator(world, dstPrivateKey, NCG * 10, height++);
        world = EnsureTombstonedValidator(world, dstPrivateKey, height++);

        // When
        var repository = new ValidatorRepository(world, actionContext);
        var srcValidator = repository.GetValidatorDelegatee(srcPrivateKey.Address);
        var bone = repository.GetBond(srcValidator, srcPrivateKey.Address);
        var redelegateValidator = new RedelegateValidator(
            srcPrivateKey.Address, dstPrivateKey.Address, bone.Share);
        actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = srcPrivateKey.Address,
            BlockIndex = height++,
        };

        // Then
        Assert.Throws<InvalidOperationException>(
            () => redelegateValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_SrcAndDstAddressAreSame_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var delegatorKey = new PrivateKey();
        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey, NCG * 10, height++);
        world = EnsurePromotedValidator(world, validatorKey, NCG * 10, height++);
        world = EnsureToMintAsset(world, delegatorKey, NCG * 10, height++);
        world = EnsureBondedDelegator(world, delegatorKey, validatorKey, NCG * 10, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            BlockIndex = height++,
            Signer = delegatorKey.Address,
        };
        var redelegateValidator = new RedelegateValidator(
            validatorKey.Address, validatorKey.Address, 10);

        // Then
        Assert.Throws<InvalidOperationException>(
            () => redelegateValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_CannotBeJailedDueToDelegatorRedelegating()
    {
        // Given
        var world = World;
        var validatorKey1 = new PrivateKey();
        var validatorKey2 = new PrivateKey();
        var delegatorKey = new PrivateKey();
        var validatorGold = NCG * 10;
        var delegatorGold = NCG * 10;
        var delegatorBalance = NCG * 100;
        var actionContext = new ActionContext { };

        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey1, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey1, validatorGold, height++);
        world = EnsureToMintAsset(world, validatorKey2, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey2, validatorGold, height++);
        world = EnsureToMintAsset(world, delegatorKey, delegatorBalance, height++);
        world = EnsureBondedDelegator(world, delegatorKey, validatorKey1, delegatorGold, height++);
        world = EnsureUnbondingDelegator(world, validatorKey1, validatorKey1, 10, height++);
        world = EnsureUnjailedValidator(world, validatorKey1, ref height);
        height++;

        // When
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetValidatorDelegatee(validatorKey1.Address);
        var expectedJailed = expectedDelegatee.Jailed;

        var redelegateValidator = new RedelegateValidator(
            srcValidatorDelegatee: validatorKey1.Address,
            dstValidatorDelegatee: validatorKey2.Address,
            share: 10);
        actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
            BlockIndex = height,
        };
        world = redelegateValidator.Execute(actionContext);

        // Then
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualDelegatee = actualRepository.GetValidatorDelegatee(validatorKey1.Address);
        var actualJailed = actualDelegatee.Jailed;

        Assert.False(actualJailed);
        Assert.Equal(expectedJailed, actualJailed);
    }
}
