#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation
{
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
            const int length = 10;
            var world = World;
            var privateKeys = GetRandomArray(length, _ => new PrivateKey());
            var favs = GetRandomArray(length, i => NCG * Random.Shared.Next(1, length + 1));

            for (int i = 0; i < length; i++)
            {
                var signer = privateKeys[i];
                var fav = favs[i];
                var promoteValidator = new PromoteValidator(signer.PublicKey, fav);
                var actionContext = new ActionContext
                {
                    PreviousState = world.MintAsset(new ActionContext(), signer.Address, NCG * 1000),
                    Signer = signer.Address,
                    BlockIndex = 10L,
                };
                world = promoteValidator.Execute(actionContext);
            }

            var blockActionContext = new ActionContext
            {
                BlockIndex = 10L,
                PreviousState = world,
                Signer = AdminKey.Address,
            };
            var expectedRepository = new ValidatorRepository(world, blockActionContext);
            var expectedValidators = expectedRepository.GetValidatorList()
                .GetBonded().OrderBy(item => item.OperatorAddress).ToList();

            world = new UpdateValidators().Execute(blockActionContext);

            var actualValidators = world.GetValidatorSet().Validators;
            Assert.Equal(expectedValidators.Count, actualValidators.Count);
            for (var i = 0; i < expectedValidators.Count; i++)
            {
                var expectedValidator = expectedValidators[i];
                var actualValidator = actualValidators[i];
                Assert.Equal(expectedValidator, actualValidator);
            }
        }
    }
}
