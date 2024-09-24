#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation
{
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
            var validatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, NCG * 10, blockHeight++);
            world = EnsureValidatorToBeJailed(world, validatorPrivateKey, ref blockHeight);

            // When
            var unjailValidator = new UnjailValidator();
            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight + SlashValidator.AbstainJailTime,
                Signer = validatorPrivateKey.PublicKey.Address,
            };
            world = unjailValidator.Execute(actionContext);

            // Then
            var repository = new ValidatorRepository(world, actionContext);
            var delegatee = repository.GetValidatorDelegatee(validatorPrivateKey.Address);
            Assert.False(delegatee.Jailed);
            Assert.Equal(-1, delegatee.JailedUntil);
            Assert.False(delegatee.Tombstoned);
        }

        [Fact]
        public void Unjail_NotExistedDelegatee_Throw()
        {
            // Given
            var world = World;
            var validatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;

            // When
            var unjailValidator = new UnjailValidator();
            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight + SlashValidator.AbstainJailTime,
                Signer = validatorPrivateKey.Address,
            };

            // Then
            Assert.Throws<FailedLoadStateException>(
                () => unjailValidator.Execute(actionContext));
        }

        [Fact]
        public void Unjail_JaliedValidator_NotJailed_Throw()
        {
            // Given
            var world = World;
            var validatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, NCG * 10, blockHeight++);

            // When
            var unjailValidator = new UnjailValidator();
            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight + SlashValidator.AbstainJailTime,
                Signer = validatorPrivateKey.PublicKey.Address,
            };

            // Then
            Assert.Throws<InvalidOperationException>(
                () => unjailValidator.Execute(actionContext));
        }

        [Fact]
        public void Unjail_JaliedValidator_Early_Throw()
        {
            // Given
            var world = World;
            var validatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, NCG * 10, blockHeight++);
            world = EnsureValidatorToBeJailed(world, validatorPrivateKey, ref blockHeight);

            // When
            var unjailValidator = new UnjailValidator();
            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight + SlashValidator.AbstainJailTime - 1,
                Signer = validatorPrivateKey.PublicKey.Address,
            };

            // Then
            Assert.Throws<InvalidOperationException>(
                () => unjailValidator.Execute(actionContext));
        }

        [Fact]
        public void Unjail_JaliedValidator_Tombstoned_Throw()
        {
            // Given
            var world = World;
            var validatorPrivateKey = new PrivateKey();
            var blockHeight = 1L;
            world = EnsureValidatorToBePromoted(
                world, validatorPrivateKey, NCG * 10, blockHeight++);
            world = EnsureValidatorToBeTombstoned(world, validatorPrivateKey, blockHeight++);

            // When
            var unjailValidator = new UnjailValidator();
            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight + SlashValidator.AbstainJailTime,
                Signer = validatorPrivateKey.PublicKey.Address,
            };

            // Then
            Assert.Throws<InvalidOperationException>(
                () => unjailValidator.Execute(actionContext));
        }
    }
}
