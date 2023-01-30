namespace Lib9c.Tests.Action.Factory
{
    using System;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Action.Factory;
    using Nekoyume.BlockChain.Policy;
    using Xunit;

    public class CombinationConsumableFactoryTest
    {
        [Theory]
        [InlineData(0L, typeof(CombinationConsumable7))]
        [InlineData(BlockPolicySource.V100080ObsoleteIndex, typeof(CombinationConsumable7))]
        [InlineData(BlockPolicySource.V100080ObsoleteIndex + 1, typeof(CombinationConsumable))]
        public void CreateByBlockIndex(long blockIndex, Type expectedType)
        {
            var avatarAddress = new PrivateKey().ToAddress();
            var slotIndex = 0;
            var recipeId = 0;

            var action =
                CombinationConsumableFactory.Create(blockIndex, avatarAddress, slotIndex, recipeId);
            Assert.Equal(expectedType, action.GetType());
            Assert.Equal(avatarAddress, action.AvatarAddress);
            Assert.Equal(slotIndex, action.SlotIndex);
            Assert.Equal(recipeId, action.RecipeId);
        }

        [Theory]
        [InlineData("combination_consumable8", typeof(CombinationConsumable))]
        [InlineData("combination_consumable7", typeof(CombinationConsumable7))]
        [InlineData("combination_consumable6", typeof(CombinationConsumable6))]
        [InlineData("combination_consumable5", typeof(CombinationConsumable5))]
        [InlineData("combination_consumable4", typeof(CombinationConsumable4))]
        [InlineData("combination_consumable3", typeof(CombinationConsumable3))]
        [InlineData("combination_consumable2", typeof(CombinationConsumable2))]
        [InlineData("combination_consumable", typeof(CombinationConsumable0))]
        public void CreateByActionType(string actionType, Type expectedType)
        {
            var avatarAddress = new PrivateKey().ToAddress();
            var slotIndex = 0;
            var recipeId = 0;

            var action =
                CombinationConsumableFactory.Create(actionType, avatarAddress, slotIndex, recipeId);
            Assert.Equal(expectedType, action.GetType());
            Assert.Equal(avatarAddress, action.AvatarAddress);
            Assert.Equal(slotIndex, action.SlotIndex);
            Assert.Equal(recipeId, action.RecipeId);
        }
    }
}
