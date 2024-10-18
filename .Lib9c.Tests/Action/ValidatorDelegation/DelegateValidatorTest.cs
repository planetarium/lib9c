namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class DelegateValidatorTest : ValidatorDelegationTestBase
{
    private interface IDelegateValidatorFixture
    {
        ValidatorInfo ValidatorInfo { get; }
    }

    public static IEnumerable<object[]> RandomSeeds => new List<object[]>
    {
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
    };

    [Fact]
    public void Serialization()
    {
        var address = new PrivateKey().Address;
        var gold = DelegationCurrency * 10;
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
        var height = 1L;
        var validatorGold = DelegationCurrency * 10;
        var delegatorGold = DelegationCurrency * 20;
        world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
        world = EnsureToMintAsset(world, validatorKey, DelegationCurrency * 100, height++);

        // When
        var delegateValidator = new DelegateValidator(validatorKey.Address, delegatorGold);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
            BlockIndex = height++,
        };
        world = delegateValidator.Execute(actionContext);

        // Then
        var repository = new ValidatorRepository(world, actionContext);
        var validator = repository.GetValidatorDelegatee(validatorKey.Address);
        var bond = repository.GetBond(validator, validatorKey.Address);
        var validatorList = repository.GetValidatorList();

        Assert.Equal(validatorGold.RawValue + delegatorGold.RawValue, bond.Share);
        Assert.Equal(validatorGold.RawValue + delegatorGold.RawValue, validator.Validator.Power);
        Assert.Equal(validator.Validator, Assert.Single(validatorList.Validators));
        Assert.Equal(DelegationCurrency * 80, world.GetBalance(validatorKey.Address, DelegationCurrency));
    }

    [Fact]
    public void Execute_Fact()
    {
        var fixture = new StaticFixture
        {
            ValidatorInfo = new ValidatorInfo
            {
                Key = new PrivateKey(),
                Cash = DelegationCurrency * 10,
                Balance = DelegationCurrency * 100,
            },
        };
        ExecuteWithFixture(fixture);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1181126949)]
    [InlineData(793705868)]
    public void Execute_Fact_WithStaticSeed(int randomSeed)
    {
        var fixture = new RandomFixture(randomSeed);
        ExecuteWithFixture(fixture);
    }

    [Theory]
    [MemberData(nameof(RandomSeeds))]
    public void Execute_Fact_WithRandomSeed(int randomSeed)
    {
        var fixture = new RandomFixture(randomSeed);
        ExecuteWithFixture(fixture);
    }

    [Fact]
    public void Execute_WithInvalidCurrency_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        var validatorGold = DelegationCurrency * 10;
        var delegatorDollar = Dollar * 20;
        world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
        world = EnsureToMintAsset(world, validatorKey, delegatorDollar, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
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
        var validatorGold = DelegationCurrency * 10;
        var delegatorGold = DelegationCurrency * 10;
        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
        world = EnsureToMintAsset(world, validatorKey, delegatorGold, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
            BlockIndex = height++,
        };
        var delegateValidator = new DelegateValidator(validatorKey.Address, DelegationCurrency * 11);

        // Then
        Assert.Throws<InsufficientBalanceException>(() => delegateValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_ToInvalidValidator_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey, DelegationCurrency * 100, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
        };
        var delegateValidator = new DelegateValidator(validatorKey.Address, DelegationCurrency * 10);

        // Then
        Assert.Throws<FailedLoadStateException>(() => delegateValidator.Execute(actionContext));
    }

    [Fact]
    public void Execute_ToTombstonedValidator_Throw()
    {
        // Given
        var world = World;
        var validatorKey = new PrivateKey();
        var height = 1L;
        var validatorGold = DelegationCurrency * 10;
        var delegatorGold = DelegationCurrency * 10;
        world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
        world = EnsureTombstonedValidator(world, validatorKey, height++);
        world = EnsureToMintAsset(world, validatorKey, delegatorGold, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
        };
        var delegateValidator = new DelegateValidator(validatorKey.Address, delegatorGold);

        // Then
        Assert.Throws<InvalidOperationException>(() => delegateValidator.Execute(actionContext));
    }

    private void ExecuteWithFixture(IDelegateValidatorFixture fixture)
    {
        // Given
        var world = World;
        var validatorKey = fixture.ValidatorInfo.Key;
        var height = 1L;
        var validatorCashToDelegate = fixture.ValidatorInfo.CashToDelegate;
        var validatorCash = fixture.ValidatorInfo.Cash;
        var validatorBalance = fixture.ValidatorInfo.Balance;
        var actionContext = new ActionContext { };
        world = EnsureToMintAsset(world, validatorKey, validatorBalance, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorCash, height++);

        // When
        var expectedRepository = new ValidatorRepository(world, new ActionContext());
        var expectedValidator = expectedRepository.GetValidatorDelegatee(validatorKey.Address);
        var expectedValidatorBalance = validatorBalance - validatorCash - validatorCashToDelegate;
        var expectedPower = validatorCash.RawValue + validatorCashToDelegate.RawValue;

        var delegateValidator = new DelegateValidator(validatorKey.Address, validatorCashToDelegate);
        actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
            BlockIndex = height++,
        };
        world = delegateValidator.Execute(actionContext);

        // Then
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualValidator = actualRepository.GetValidatorDelegatee(validatorKey.Address);
        var actualValidatorBalance = world.GetBalance(validatorKey.Address, DelegationCurrency);
        var actualPower = actualValidator.Power;
        var actualBond = actualRepository.GetBond(actualValidator, validatorKey.Address);

        Assert.Equal(expectedPower, actualBond.Share);
        Assert.Equal(expectedValidatorBalance, actualValidatorBalance);
        Assert.Equal(expectedPower, actualPower);
    }

    private struct ValidatorInfo
    {
        public ValidatorInfo()
        {
        }

        public ValidatorInfo(Random random)
        {
            Balance = GetRandomFAV(DelegationCurrency, random);
            Cash = GetRandomCash(random, Balance);
            CashToDelegate = GetRandomCash(random, Balance - Cash);
        }

        public PrivateKey Key { get; set; } = new PrivateKey();

        public FungibleAssetValue CashToDelegate { get; set; } = DelegationCurrency * 10;

        public FungibleAssetValue Cash { get; set; } = DelegationCurrency * 10;

        public FungibleAssetValue Balance { get; set; } = DelegationCurrency * 100;
    }

    private struct StaticFixture : IDelegateValidatorFixture
    {
        public DelegateValidator DelegateValidator { get; set; }

        public ValidatorInfo ValidatorInfo { get; set; }
    }

    private class RandomFixture : IDelegateValidatorFixture
    {
        private readonly Random _random;

        public RandomFixture(int randomSeed)
        {
            _random = new Random(randomSeed);
            ValidatorInfo = new ValidatorInfo(_random);
        }

        public ValidatorInfo ValidatorInfo { get; }
    }
}
