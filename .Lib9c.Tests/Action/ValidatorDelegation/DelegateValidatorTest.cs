namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;
using Org.BouncyCastle.Crypto.Modes;
using Xunit;

public class DelegateValidatorTest : ValidatorDelegationTestBase
{
    private interface IDelegateValidatorFixture
    {
        ValidatorInfo ValidatorInfo { get; }

        DelegatorInfo[] DelegatorInfos { get; }
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
        var gold = GG * 10;
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
        var validatorGold = GG * 10;
        var delegatorGold = GG * 20;
        world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
        world = EnsureToMintAsset(world, delegatorKey, GG * 100, height++);

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
        Assert.Equal(GG * 80, world.GetBalance(delegatorKey.Address, GG));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(9)]
    public void Execute_Theory(int delegatorCount)
    {
        var fixture = new StaticFixture
        {
            ValidatorInfo = new ValidatorInfo
            {
                Key = new PrivateKey(),
                Cash = GG * 10,
                Balance = GG * 100,
            },
            DelegatorInfos = Enumerable.Range(0, delegatorCount)
                .Select(_ => new DelegatorInfo
                {
                    Key = new PrivateKey(),
                    Cash = GG * 20,
                    Balance = GG * 100,
                })
                .ToArray(),
        };
        ExecuteWithFixture(fixture);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1181126949)]
    [InlineData(793705868)]
    public void Execute_Theory_WithStaticSeed(int randomSeed)
    {
        var fixture = new RandomFixture(randomSeed);
        ExecuteWithFixture(fixture);
    }

    [Theory]
    [MemberData(nameof(RandomSeeds))]
    public void Execute_Theory_WithRandomSeed(int randomSeed)
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
        var delegatorKey = new PrivateKey();
        var height = 1L;
        var validatorGold = GG * 10;
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
        var validatorGold = GG * 10;
        var delegatorGold = GG * 10;
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
        var delegateValidator = new DelegateValidator(validatorKey.Address, GG * 11);

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
        world = EnsureToMintAsset(world, delegatorKey, GG * 100, height++);

        // When
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = delegatorKey.Address,
        };
        var delegateValidator = new DelegateValidator(validatorKey.Address, GG * 10);

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
        var validatorGold = GG * 10;
        var delegatorGold = GG * 10;
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
        var validatorCash = GG * 10;
        var validatorGold = GG * 100;
        var delegatorGold = GG * 10;
        var delegatorBalance = GG * 100;
        var actionContext = new ActionContext { };

        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorCash, height++);
        world = EnsureUnbondingDelegator(world, validatorKey, validatorKey, 10, height++);
        world = EnsureBondedDelegator(world, validatorKey, validatorKey, validatorCash, height++);

        world = EnsureToMintAsset(world, delegatorKey, delegatorBalance, height++);
        world = EnsureBondedDelegator(world, delegatorKey, validatorKey, delegatorGold, height++);
        world = EnsureUnjailedValidator(world, validatorKey, ref height);

        // When
        var expectedRepository = new ValidatorRepository(world, actionContext);
        var expectedDelegatee = expectedRepository.GetValidatorDelegatee(validatorKey.Address);
        var expectedJailed = expectedDelegatee.Jailed;

        var delegateValidator = new DelegateValidator(validatorKey.Address, 1 * GG);
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

    private void ExecuteWithFixture(IDelegateValidatorFixture fixture)
    {
        // Given
        var world = World;
        var validatorKey = fixture.ValidatorInfo.Key;
        var height = 1L;
        var validatorCash = fixture.ValidatorInfo.Cash;
        var validatorBalance = fixture.ValidatorInfo.Balance;
        var delegatorKeys = fixture.DelegatorInfos.Select(i => i.Key).ToArray();
        var delegatorCashes = fixture.DelegatorInfos.Select(i => i.Cash).ToArray();
        var delegatorBalances = fixture.DelegatorInfos.Select(i => i.Balance).ToArray();
        var actionContext = new ActionContext { };
        world = EnsureToMintAsset(world, validatorKey, validatorBalance, height++);
        world = EnsurePromotedValidator(world, validatorKey, validatorCash, height++);
        world = EnsureToMintAssets(world, delegatorKeys, delegatorBalances, height++);

        // When
        var expectedRepository = new ValidatorRepository(world, new ActionContext());
        var expectedValidator = expectedRepository.GetValidatorDelegatee(validatorKey.Address);
        var expectedValidatorBalance = validatorBalance - validatorCash;
        var expectedDelegatorBalances = delegatorBalances
            .Select((b, i) => b - delegatorCashes[i]).ToArray();
        var expectedPower = delegatorCashes.Aggregate(
            validatorCash.RawValue, (a, b) => a + b.RawValue);

        for (var i = 0; i < delegatorKeys.Length; i++)
        {
            var delegateValidator = new DelegateValidator(validatorKey.Address, delegatorCashes[i]);
            actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = delegatorKeys[i].Address,
                BlockIndex = height++,
            };
            world = delegateValidator.Execute(actionContext);
        }

        // Then
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualValidator = actualRepository.GetValidatorDelegatee(validatorKey.Address);
        var actualValidatorBalance = world.GetBalance(validatorKey.Address, GG);
        var actualDelegatorBalances = delegatorKeys
            .Select(k => world.GetBalance(k.Address, GG)).ToArray();
        var actualPower = actualValidator.Power;

        for (var i = 0; i < delegatorKeys.Length; i++)
        {
            var actualBond = actualRepository.GetBond(actualValidator, delegatorKeys[i].Address);
            Assert.Contains(delegatorKeys[i].Address, actualValidator.Delegators);
            Assert.Equal(delegatorCashes[i].RawValue, actualBond.Share);
            Assert.Equal(expectedDelegatorBalances[i], actualDelegatorBalances[i]);
        }

        Assert.Equal(expectedValidatorBalance, actualValidatorBalance);
        Assert.Equal(expectedPower, actualPower);
        Assert.Equal(expectedDelegatorBalances, actualDelegatorBalances);
    }

    private struct ValidatorInfo
    {
        public ValidatorInfo()
        {
        }

        public ValidatorInfo(Random random)
        {
            Balance = GetRandomGG(random);
            Cash = GetRandomCash(random, Balance);
        }

        public PrivateKey Key { get; set; } = new PrivateKey();

        public FungibleAssetValue Cash { get; set; } = GG * 10;

        public FungibleAssetValue Balance { get; set; } = GG * 100;
    }

    private struct DelegatorInfo
    {
        public DelegatorInfo()
        {
        }

        public DelegatorInfo(Random random)
        {
            Balance = GetRandomGG(random);
            Cash = GetRandomCash(random, Balance);
        }

        public PrivateKey Key { get; set; } = new PrivateKey();

        public FungibleAssetValue Cash { get; set; } = GG * 10;

        public FungibleAssetValue Balance { get; set; } = GG * 100;
    }

    private struct StaticFixture : IDelegateValidatorFixture
    {
        public DelegateValidator DelegateValidator { get; set; }

        public ValidatorInfo ValidatorInfo { get; set; }

        public DelegatorInfo[] DelegatorInfos { get; set; }
    }

    private class RandomFixture : IDelegateValidatorFixture
    {
        private readonly Random _random;

        public RandomFixture(int randomSeed)
        {
            _random = new Random(randomSeed);
            ValidatorInfo = new ValidatorInfo(_random);
            DelegatorInfos = Enumerable.Range(0, _random.Next(1, 10))
                .Select(_ => new DelegatorInfo(_random))
                .ToArray();
        }

        public ValidatorInfo ValidatorInfo { get; }

        public DelegatorInfo[] DelegatorInfos { get; }
    }
}
