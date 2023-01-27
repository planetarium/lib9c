#nullable enable

namespace Lib9c.Tests.Action.Factory
{
    using System;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Action.Factory;
    using Nekoyume.Action.Interface;
    using Nekoyume.BlockChain.Policy;
    using Xunit;

    public class CombinationEquipmentFactoryTest
    {
        [Theory]
        [InlineData(BlockPolicySource.V100282ObsoleteIndex - 1, typeof(CombinationEquipment13))]
        [InlineData(BlockPolicySource.V100282ObsoleteIndex, typeof(CombinationEquipment13))]
        [InlineData(BlockPolicySource.V100282ObsoleteIndex + 1, typeof(CombinationEquipment))]
        public void CreateByBlockIndex(long blockIndex, Type type)
        {
            var address = new PrivateKey().ToAddress();
            var slotIndex = 0;
            var recipeId = 1;
            int? subRecipeId = null;
            var payByCrystal = false;
            var useHammerPoint = false;
            var action = CombinationEquipmentFactory.CreateByBlockIndex(
                blockIndex,
                address,
                slotIndex,
                recipeId,
                subRecipeId,
                payByCrystal,
                useHammerPoint
            );
            Assert.Equal(type, action.GetType());
            Assert.Equal(address, ((ICombinationEquipmentV3)action).AvatarAddress);
            Assert.Equal(slotIndex, ((ICombinationEquipmentV3)action).SlotIndex);
            Assert.Equal(recipeId, ((ICombinationEquipmentV3)action).RecipeId);
            Assert.Equal(subRecipeId, ((ICombinationEquipmentV3)action).SubRecipeId);
            Assert.Equal(payByCrystal, ((ICombinationEquipmentV3)action).PayByCrystal);
            Assert.Equal(useHammerPoint, ((ICombinationEquipmentV3)action).UseHammerPoint);
        }

        [Theory]
        [InlineData(13, typeof(CombinationEquipment13))]
        [InlineData(14, typeof(CombinationEquipment))]
        public void CreateByVersion_Success(int version, Type type)
        {
            var address = new PrivateKey().ToAddress();
            var slotIndex = 0;
            var recipeId = 1;
            int? subRecipeId = null;
            var payByCrystal = false;
            var useHammerPoint = false;
            var action = CombinationEquipmentFactory.CreateByVersion(
                version,
                address,
                slotIndex,
                recipeId,
                subRecipeId,
                payByCrystal,
                useHammerPoint
            );
            Assert.Equal(type, action.GetType());
            Assert.Equal(address, ((ICombinationEquipmentV3)action).AvatarAddress);
            Assert.Equal(slotIndex, ((ICombinationEquipmentV3)action).SlotIndex);
            Assert.Equal(recipeId, ((ICombinationEquipmentV3)action).RecipeId);
            Assert.Equal(subRecipeId, ((ICombinationEquipmentV3)action).SubRecipeId);
            Assert.Equal(payByCrystal, ((ICombinationEquipmentV3)action).PayByCrystal);
            Assert.Equal(useHammerPoint, ((ICombinationEquipmentV3)action).UseHammerPoint);
        }

        [Theory]
        [InlineData(12)]
        [InlineData(15)]
        public void CreateByVersion_Fail(int version)
        {
            var address = new PrivateKey().ToAddress();
            var slotIndex = 0;
            var recipeId = 1;
            int? subRecipeId = null;
            var payByCrystal = false;
            var useHammerPoint = false;
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CombinationEquipmentFactory.CreateByVersion(
                    version,
                    address,
                    slotIndex,
                    recipeId,
                    subRecipeId,
                    payByCrystal,
                    useHammerPoint
                )
            );
        }
    }
}
