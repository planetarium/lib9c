namespace Lib9c.Tests.Action.ValidatorDelegation
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using Libplanet.Crypto;
    using Nekoyume.Action.ValidatorDelegation;
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

        private static readonly long CommissionPercentageChangeCooldown
            = ValidatorDelegatee.CommissionPercentageUpdateCooldown;

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

        public static IEnumerable<object[]> InvalidCommisionPercentageCooldown => new List<object[]>
        {
            new object[] { 0 },
            new object[] { CommissionPercentageChangeCooldown - 1 },
        };

        public static IEnumerable<object[]> ValidCommisionPercentagePeriod => new List<object[]>
        {
            new object[] { CommissionPercentageChangeCooldown },
            new object[] { CommissionPercentageChangeCooldown + 1 },
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
            var validatorGold = DelegationCurrency * 10;
            var height = 1L;
            world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
            world = EnsurePromotedValidator(world, validatorKey, validatorGold, height);

            // When
            var setValidatorCommission = new SetValidatorCommission(
                validatorKey.Address, commissionPercentage: 11);
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = validatorKey.Address,
                BlockIndex = height + CommissionPercentageChangeCooldown,
            };
            world = setValidatorCommission.Execute(actionContext);

            // Then
            var actualRepository = new ValidatorRepository(world, actionContext);
            var actualDelegatee = actualRepository.GetValidatorDelegatee(validatorKey.Address);
            var actualPercentage = actualDelegatee.CommissionPercentage;

            Assert.Equal(11, actualPercentage);
        }

        [Theory]
        [InlineData(9, 10)]
        [InlineData(9, 8)]
        [InlineData(0, 1)]
        [InlineData(20, 19)]
        public void Execute_Theory(int oldCommissionPercentage, int newCommissionPercentage)
        {
            // Given
            var world = World;
            var validatorKey = new PrivateKey();
            var validatorGold = DelegationCurrency * 10;
            var height = 1L;
            world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
            world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
            world = EnsureCommissionChangedValidator(
                world, validatorKey, oldCommissionPercentage, ref height);

            // When
            var setValidatorCommission = new SetValidatorCommission(
                validatorKey.Address,
                newCommissionPercentage);
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = validatorKey.Address,
                BlockIndex = height + CommissionPercentageChangeCooldown,
            };
            world = setValidatorCommission.Execute(actionContext);

            // Then
            var actualRepository = new ValidatorRepository(world, actionContext);
            var actualDelegatee = actualRepository.GetValidatorDelegatee(validatorKey.Address);
            var actualPercentage = actualDelegatee.CommissionPercentage;

            Assert.Equal(newCommissionPercentage, actualPercentage);
        }

        [Theory]
        [MemberData(nameof(RandomInvalidCommisionPercentage))]
        public void Execute_Theory_WithValueGreaterThanMaximum_Throw(int commissionPercentage)
        {
            // Given
            var world = World;
            var validatorKey = new PrivateKey();
            var validatorGold = DelegationCurrency * 10;
            var height = 1L;

            world = EnsureToMintAsset(world, validatorKey, DelegationCurrency * 10, height++);
            world = EnsurePromotedValidator(world, validatorKey, DelegationCurrency * 10, height);

            // When
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = validatorKey.Address,
                BlockIndex = height + CommissionPercentageChangeCooldown,
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
            var validatorGold = DelegationCurrency * 10;
            var height = 1L;

            world = EnsureToMintAsset(world, validatorKey, DelegationCurrency * 10, height++);
            world = EnsurePromotedValidator(world, validatorKey, DelegationCurrency * 10, height++);

            // When
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = validatorKey.Address,
                BlockIndex = height + CommissionPercentageChangeCooldown,
            };
            var setValidatorCommission = new SetValidatorCommission(
                validatorKey.Address,
                commissionPercentage);

            // Then
            Assert.Throws<ArgumentOutOfRangeException>(
                () => setValidatorCommission.Execute(actionContext));
        }

        [Theory]
        [MemberData(nameof(InvalidCommisionPercentageCooldown))]
        public void Execute_Theory_WithInvalidValue_Throw(int cooldown)
        {
            // Given
            var world = World;
            var validatorKey = new PrivateKey();
            var validatorGold = DelegationCurrency * 10;
            var height = 1L;
            world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
            world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
            world = EnsureCommissionChangedValidator(world, validatorKey, 15, ref height);

            // When
            var actionContext = new ActionContext
            {
                PreviousState = world,
                Signer = validatorKey.Address,
                BlockIndex = height + cooldown,
            };
            var setValidatorCommission = new SetValidatorCommission(
                validatorKey.Address, commissionPercentage: 14);

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
            var validatorGold = DelegationCurrency * 10;
            var height = 1L;
            world = EnsureToMintAsset(world, validatorKey, validatorGold, height++);
            world = EnsurePromotedValidator(world, validatorKey, validatorGold, height++);
            world = EnsureCommissionChangedValidator(world, validatorKey, 11, ref height);

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
    }
}
