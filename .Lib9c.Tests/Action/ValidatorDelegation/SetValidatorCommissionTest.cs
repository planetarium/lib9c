namespace Lib9c.Tests.Action.ValidatorDelegation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action.ValidatorDelegation;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.ValidatorDelegation;
    using Xunit;

    public class SetValidatorCommissionTest : ValidatorDelegationTestBase
    {
        /// <summary>
        /// Tested that ValidatorDelegatee.MaxCommissionPercentage is less than int.MaxValue
        /// in ConstantTest.cs file.
        /// </summary>
        private static readonly int MaxCommissionPercentage
            = (int)ValidatorDelegatee.MaxCommissionPercentage;

        private static readonly int CommissionPercentageChangePeriod
            = 10;

        public static IEnumerable<object[]> RandomCommisionPercentage => new List<object[]>
        {
            new object[] { Random.Shared.Next(MaxCommissionPercentage) },
            new object[] { Random.Shared.Next(MaxCommissionPercentage) },
            new object[] { Random.Shared.Next(MaxCommissionPercentage) },
        };

        public static IEnumerable<object[]> RandomInvalidCommisionPercentage => new List<object[]>
        {
            new object[] { Random.Shared.Next(MaxCommissionPercentage, int.MaxValue) },
            new object[] { Random.Shared.Next(MaxCommissionPercentage, int.MaxValue) },
            new object[] { Random.Shared.Next(MaxCommissionPercentage, int.MaxValue) },
        };

        public static IEnumerable<object[]> InvalidCommisionPercentagePeriod => new List<object[]>
        {
            new object[] { 0 },
            new object[] { CommissionPercentageChangePeriod - 1 },
        };

        public static IEnumerable<object[]> ValidCommisionPercentagePeriod => new List<object[]>
        {
            new object[] { CommissionPercentageChangePeriod },
            new object[] { CommissionPercentageChangePeriod + 1 },
        };

        [Fact]
        public void Serialization()
        {
            var address = new PrivateKey().Address;
            BigInteger commissionPercentage = 10;
            var action = new SetValidatorCommission(address, commissionPercentage);
            var plainValue = action.PlainValue;

            var deserialized = new SetValidatorCommission();
            deserialized.LoadPlainValue(plainValue);
            Assert.Equal(address, deserialized.ValidatorDelegatee);
            Assert.Equal(commissionPercentage, deserialized.CommissionPercentage);
        }

        [Fact]
        public void Execute()
        {
            // Given
            var world = World;
            var validatorKey = new PrivateKey();
            var validatorGold = NCG * 10;
            var height = 1L;
            world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
            world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);

            // When
            var setValidatorCommission = new SetValidatorCommission(
                validatorKey.Address, commissionPercentage: 11);
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = validatorKey.Address,
                BlockIndex = height++,
            };
            world = setValidatorCommission.Execute(actionContext);

            // Then
            var actualRepository = new ValidatorRepository(world, actionContext);
            var actualDelegatee = actualRepository.GetValidatorDelegatee(validatorKey.Address);
            var actualPercentage = actualDelegatee.CommissionPercentage;

            Assert.Equal(11, actualPercentage);
        }

        [Theory]
        // [MemberData(nameof(RandomCommisionPercentage))]
        [InlineData(9)]
        [InlineData(11)]
        public void Execute_Theory(int commissionPercentage)
        {
            // Given
            var world = World;
            var validatorKey = new PrivateKey();
            var validatorGold = NCG * 10;
            var height = 1L;
            world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
            world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);

            // When
            var setValidatorCommission = new SetValidatorCommission(
                validatorKey.Address,
                commissionPercentage);
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = validatorKey.Address,
                BlockIndex = height++,
            };
            world = setValidatorCommission.Execute(actionContext);

            // Then
            var actualRepository = new ValidatorRepository(world, actionContext);
            var actualDelegatee = actualRepository.GetValidatorDelegatee(validatorKey.Address);
            var actualPercentage = actualDelegatee.CommissionPercentage;

            Assert.Equal(commissionPercentage, actualPercentage);
        }

        [Theory]
        [MemberData(nameof(RandomInvalidCommisionPercentage))]
        public void Execute_Theory_WithValueGreaterThanMaximum_Throw(int commissionPercentage)
        {
            // Given
            var world = World;
            var validatorKey = new PrivateKey();
            var validatorGold = NCG * 10;
            var height = 1L;

            world = EnsureToMintAsset(world, validatorKey, NCG * 10, height++);
            world = EnsurePromotedValidator(world, validatorKey, NCG * 10, height++);

            // When
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = validatorKey.Address,
                BlockIndex = height++,
            };
            var setValidatorCommission = new SetValidatorCommission(
                validatorKey.Address,
                commissionPercentage);

            // Then
            Assert.Throws<ArgumentOutOfRangeException>(
                () => setValidatorCommission.Execute(actionContext));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-2)]
        public void Execute_Theory_WithNegative_Throw(int commissionPercentage)
        {
            // Given
            var world = World;
            var validatorKey = new PrivateKey();
            var validatorGold = NCG * 10;
            var height = 1L;

            world = EnsureToMintAsset(world, validatorKey, NCG * 10, height++);
            world = EnsurePromotedValidator(world, validatorKey, NCG * 10, height++);

            // When
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = validatorKey.Address,
                BlockIndex = height++,
            };
            var setValidatorCommission = new SetValidatorCommission(
                validatorKey.Address,
                commissionPercentage);

            // Then
            Assert.Throws<ArgumentOutOfRangeException>(
                () => setValidatorCommission.Execute(actionContext));
        }

        [Theory]
        [MemberData(nameof(InvalidCommisionPercentagePeriod))]
        public void Execute_Theory_WithInvalidValue_Throw(int period)
        {
            // Given
            var world = World;
            var validatorKey = new PrivateKey();
            var validatorGold = NCG * 10;
            var height = 1L;
            world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
            world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
            world = EnsureCommissionChangedValidator(world, validatorKey, 11, height);

            // When
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = validatorKey.Address,
                BlockIndex = height + period,
            };
            var setValidatorCommission = new SetValidatorCommission(
                validatorKey.Address, commissionPercentage: 12);

            // Then
            Assert.Throws<InvalidOperationException>(
                () => setValidatorCommission.Execute(actionContext));
        }

        [Theory]
        [MemberData(nameof(ValidCommisionPercentagePeriod))]
        public void Execute_Theory_WitValue(int period)
        {
            // Given
            var world = World;
            var validatorKey = new PrivateKey();
            var validatorGold = NCG * 10;
            var height = 1L;
            world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
            world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
            world = EnsureCommissionChangedValidator(world, validatorKey, 11, height);

            // When
            var expectedCommission = 12;
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = validatorKey.Address,
                BlockIndex = height + period,
            };
            var setValidatorCommission = new SetValidatorCommission(
                validatorKey.Address, commissionPercentage: expectedCommission);
            world = setValidatorCommission.Execute(actionContext);

            // Then
            var actualRepository = new ValidatorRepository(world, actionContext);
            var actualDelegatee = actualRepository.GetValidatorDelegatee(validatorKey.Address);
            var actualPercentage = actualDelegatee.CommissionPercentage;

            Assert.Equal(expectedCommission, actualPercentage);
        }

        [Fact]
        public void CannotExceedMaxPercentage()
        {
            IWorld world = new World(MockUtil.MockModernWorldState);
            var context = new ActionContext { };
            var ncg = Currency.Uncapped("NCG", 2, null);
            // TODO: Use Currencies.GuildGold when it's available.
            // var gg = Currencies.GuildGold;
            var gg = ncg;
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            var validatorPublicKey = new PrivateKey().PublicKey;
            world = world.MintAsset(context, validatorPublicKey.Address, gg * 100);
            var promoteFAV = gg * 10;
            world = new PromoteValidator(validatorPublicKey, promoteFAV).Execute(new ActionContext
            {
                PreviousState = world,
                Signer = validatorPublicKey.Address,
            });

            foreach (var percentage in Enumerable.Range(11, 10))
            {
                world = new SetValidatorCommission(validatorPublicKey.Address, percentage).Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = validatorPublicKey.Address,
                });
            }

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SetValidatorCommission(validatorPublicKey.Address, 31).Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = validatorPublicKey.Address,
                }));
        }

        [Fact]
        public void CannotExceedMaxChange()
        {
            IWorld world = new World(MockUtil.MockModernWorldState);
            var context = new ActionContext { };
            var ncg = Currency.Uncapped("NCG", 2, null);
            // TODO: Use Currencies.GuildGold when it's available.
            // var gg = Currencies.GuildGold;
            var gg = ncg;
            var goldCurrencyState = new GoldCurrencyState(ncg);
            world = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            var validatorPublicKey = new PrivateKey().PublicKey;
            world = world.MintAsset(context, validatorPublicKey.Address, gg * 100);
            var promoteFAV = gg * 10;
            world = new PromoteValidator(validatorPublicKey, promoteFAV).Execute(new ActionContext
            {
                PreviousState = world,
                Signer = validatorPublicKey.Address,
            });

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SetValidatorCommission(validatorPublicKey.Address, 12).Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = validatorPublicKey.Address,
                }));
        }
    }
}
