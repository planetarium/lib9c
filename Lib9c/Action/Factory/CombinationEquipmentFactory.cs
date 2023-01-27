using System;
using Libplanet;
using Nekoyume.Action.Interface;
using Nekoyume.BlockChain.Policy;

namespace Nekoyume.Action.Factory
{
    public static class CombinationEquipmentFactory
    {
        public static ICombinationEquipmentFamily CreateByBlockIndex(
            long blockIndex,
            Address avatarAddress,
            int slotIndex,
            int recipeId,
            int? subRecipeId,
            bool payByCrystal,
            bool useHammerPoint
        )
        {
            if (blockIndex > BlockPolicySource.V100282ObsoleteIndex)
            {
                return new CombinationEquipment
                {
                    avatarAddress = avatarAddress,
                    slotIndex = slotIndex,
                    recipeId = recipeId,
                    subRecipeId = subRecipeId,
                    payByCrystal = payByCrystal,
                    useHammerPoint = useHammerPoint
                };
            }

            return new CombinationEquipment13
            {
                avatarAddress = avatarAddress,
                slotIndex = slotIndex,
                recipeId = recipeId,
                subRecipeId = subRecipeId,
                payByCrystal = payByCrystal,
                useHammerPoint = useHammerPoint
            };
        }

        public static ICombinationEquipmentFamily CreateByVersion(
            int version,
            Address avatarAddress,
            int slotIndex,
            int recipeId,
            int? subRecipeId,
            bool payByCrystal,
            bool useHammerPoint
        ) => version switch
        {
            14 => new CombinationEquipment
            {
                avatarAddress = avatarAddress,
                slotIndex = slotIndex,
                recipeId = recipeId,
                subRecipeId = subRecipeId,
                payByCrystal = payByCrystal,
                useHammerPoint = useHammerPoint
            },
            13 => new CombinationEquipment13
            {
                avatarAddress = avatarAddress,
                slotIndex = slotIndex,
                recipeId = recipeId,
                subRecipeId = subRecipeId,
                payByCrystal = payByCrystal,
                useHammerPoint = useHammerPoint
            },
            _ => throw new ArgumentOutOfRangeException($"Invalid version: {version}"),
        };
    }
}
