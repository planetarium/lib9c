namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Action.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class RedelegateValidatorTest : ValidatorDelegationTestBase
{
    private interface IRedelegateValidatorFixture
    {
        ValidatorInfo ValidatorInfo1 { get; }

        ValidatorInfo ValidatorInfo2 { get; }

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

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(9)]
    public void Execute_Theory(int delegatorCount)
    {
        var fixture = new StaticFixture
        {
            ValidatorInfo1 = new ValidatorInfo
            {
                Key = new PrivateKey(),
                Cash = NCG * 10,
                Balance = NCG * 100,
            },
            ValidatorInfo2 = new ValidatorInfo
            {
                Key = new PrivateKey(),
                Cash = NCG * 10,
                Balance = NCG * 100,
            },
            DelegatorInfos = Enumerable.Range(0, delegatorCount)
                .Select(_ => new DelegatorInfo
                {
                    Key = new PrivateKey(),
                    Cash = NCG * 20,
                    Balance = NCG * 100,
                    Redelegating = 20,
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
    public void Execute_FromJailedValidator_ToNotJailedValidator()
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
        Assert.Equal(expectedBond2.Share + 9, actualBond2.Share);
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
        var validatorCash = NCG * 10;
        var validatorGold = NCG * 100;
        var delegatorGold = NCG * 10;
        var delegatorBalance = NCG * 100;
        var actionContext = new ActionContext { };

        var height = 1L;
        world = EnsureToMintAsset(world, validatorKey1, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey1, validatorCash, height++);
        world = EnsureUnbondingDelegator(world, validatorKey1, validatorKey1, 10, height++);
        world = EnsureBondedDelegator(world, validatorKey1, validatorKey1, validatorCash, height++);

        world = EnsureToMintAsset(world, validatorKey2, validatorGold, height++);
        world = EnsurePromotedValidator(world, validatorKey2, validatorGold, height++);
        world = EnsureToMintAsset(world, delegatorKey, delegatorBalance, height++);
        world = EnsureBondedDelegator(world, delegatorKey, validatorKey1, delegatorGold, height++);
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

    private void ExecuteWithFixture(IRedelegateValidatorFixture fixture)
    {
        // Given
        var world = World;
        var validatorKey1 = fixture.ValidatorInfo1.Key;
        var validatorKey2 = fixture.ValidatorInfo2.Key;
        var height = 1L;
        var validatorCash1 = fixture.ValidatorInfo1.Cash;
        var validatorBalance1 = fixture.ValidatorInfo1.Balance;
        var validatorCash2 = fixture.ValidatorInfo2.Cash;
        var validatorBalance2 = fixture.ValidatorInfo2.Balance;
        var delegatorKeys = fixture.DelegatorInfos.Select(i => i.Key).ToArray();
        var delegatorCashes = fixture.DelegatorInfos.Select(i => i.Cash).ToArray();
        var delegatorBalances = fixture.DelegatorInfos.Select(i => i.Balance).ToArray();
        var delegatorRedelegatings = fixture.DelegatorInfos.Select(i => i.Redelegating).ToArray();
        var actionContext = new ActionContext { };
        world = EnsureToMintAsset(world, validatorKey1, validatorBalance1, height++);
        world = EnsurePromotedValidator(world, validatorKey1, validatorCash1, height++);
        world = EnsureToMintAsset(world, validatorKey2, validatorBalance2, height++);
        world = EnsurePromotedValidator(world, validatorKey2, validatorCash2, height++);
        world = EnsureToMintAssets(world, delegatorKeys, delegatorBalances, height++);
        world = EnsureBondedDelegators(
            world, delegatorKeys, validatorKey1, delegatorCashes, height++);

        // When
        var expectedRepository = new ValidatorRepository(world, new ActionContext());
        var expectedValidator1 = expectedRepository.GetValidatorDelegatee(validatorKey1.Address);
        var expectedValidator2 = expectedRepository.GetValidatorDelegatee(validatorKey2.Address);
        var expectedValidatorBalance1 = validatorBalance1 - validatorCash1;
        var expectedValidatorBalance2 = validatorBalance2 - validatorCash2;
        var expectedDelegatorBalances = delegatorKeys
            .Select(k => world.GetBalance(k.Address, NCG)).ToArray();
        var expectedShares1 = delegatorCashes
            .Select((c, i) => c.RawValue - delegatorRedelegatings[i]).ToArray();
        var expectedShares2 = delegatorRedelegatings;
        var expectedPower1 = expectedValidator1.Power - delegatorRedelegatings.Aggregate(
            BigInteger.Zero, (a, b) => a + b);
        var expectedPower2 = expectedValidator2.Power + delegatorRedelegatings.Aggregate(
            BigInteger.Zero, (a, b) => a + b);

        for (var i = 0; i < delegatorKeys.Length; i++)
        {
            var redelegating = fixture.DelegatorInfos[i].Redelegating;
            var redelegateValidator = new RedelegateValidator(
                validatorKey1.Address, validatorKey2.Address, redelegating);
            actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = delegatorKeys[i].Address,
                BlockIndex = height++,
            };
            world = redelegateValidator.Execute(actionContext);
        }

        // Then
        var actualRepository = new ValidatorRepository(world, actionContext);
        var actualValidator1 = actualRepository.GetValidatorDelegatee(validatorKey1.Address);
        var actualValidator2 = actualRepository.GetValidatorDelegatee(validatorKey2.Address);
        var actualValidatorBalance1 = world.GetBalance(validatorKey1.Address, NCG);
        var actualValidatorBalance2 = world.GetBalance(validatorKey2.Address, NCG);
        var actualDelegatorBalances = delegatorKeys
            .Select(k => world.GetBalance(k.Address, NCG)).ToArray();
        var actualPower1 = actualValidator1.Power;
        var actualPower2 = actualValidator2.Power;

        for (var i = 0; i < delegatorKeys.Length; i++)
        {
            var actualBond1 = actualRepository.GetBond(actualValidator1, delegatorKeys[i].Address);
            var actualBond2 = actualRepository.GetBond(actualValidator2, delegatorKeys[i].Address);
            Assert.Contains(delegatorKeys[i].Address, actualValidator1.Delegators);
            Assert.Contains(delegatorKeys[i].Address, actualValidator2.Delegators);
            Assert.Equal(expectedShares1[i], actualBond1.Share);
            Assert.Equal(expectedShares2[i], actualBond2.Share);
            Assert.Equal(expectedDelegatorBalances[i], actualDelegatorBalances[i]);
        }

        Assert.Equal(expectedValidatorBalance1, actualValidatorBalance1);
        Assert.Equal(expectedValidatorBalance2, actualValidatorBalance2);
        Assert.Equal(expectedPower1, actualPower1);
        Assert.Equal(expectedPower2, actualPower2);
        Assert.Equal(expectedDelegatorBalances, actualDelegatorBalances);
    }

    private struct ValidatorInfo
    {
        public ValidatorInfo()
        {
        }

        public ValidatorInfo(Random random)
        {
            Balance = GetRandomNCG(random);
            Cash = GetRandomCash(random, Balance);
        }

        public PrivateKey Key { get; set; } = new PrivateKey();

        public FungibleAssetValue Cash { get; set; } = NCG * 10;

        public FungibleAssetValue Balance { get; set; } = NCG * 100;
    }

    private struct DelegatorInfo
    {
        public DelegatorInfo()
        {
        }

        public DelegatorInfo(Random random)
        {
            Balance = GetRandomNCG(random);
            Cash = GetRandomCash(random, Balance);
            Redelegating = GetRandomCash(random, Cash).RawValue;
        }

        public PrivateKey Key { get; set; } = new PrivateKey();

        public FungibleAssetValue Cash { get; set; } = NCG * 10;

        public FungibleAssetValue Balance { get; set; } = NCG * 100;

        public BigInteger Redelegating { get; set; } = 100;
    }

    private struct StaticFixture : IRedelegateValidatorFixture
    {
        public ValidatorInfo ValidatorInfo1 { get; set; }

        public ValidatorInfo ValidatorInfo2 { get; set; }

        public DelegatorInfo[] DelegatorInfos { get; set; }
    }

    private class RandomFixture : IRedelegateValidatorFixture
    {
        private readonly Random _random;

        public RandomFixture(int randomSeed)
        {
            _random = new Random(randomSeed);
            ValidatorInfo1 = new ValidatorInfo(_random);
            ValidatorInfo2 = new ValidatorInfo(_random);
            DelegatorInfos = Enumerable.Range(0, _random.Next(1, 10))
                .Select(_ => new DelegatorInfo(_random))
                .ToArray();
        }

        public ValidatorInfo ValidatorInfo1 { get; }

        public ValidatorInfo ValidatorInfo2 { get; }

        public DelegatorInfo[] DelegatorInfos { get; }
    }
}
