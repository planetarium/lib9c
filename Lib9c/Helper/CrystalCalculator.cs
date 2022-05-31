using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;

namespace Nekoyume.Helper
{
    public static class CrystalCalculator
    {
        public static readonly Currency CRYSTAL = new Currency("CRYSTAL", 18, minters: null);

        public static FungibleAssetValue CalculateRecipeUnlockCost(IEnumerable<int> recipeIds, EquipmentItemRecipeSheet equipmentItemRecipeSheet)
        {
            var cost = 0 * CRYSTAL;

            return recipeIds
                .Select(id => equipmentItemRecipeSheet[id])
                .Aggregate(cost, (current, row) => current + row.CRYSTAL * CRYSTAL);
        }

        public static FungibleAssetValue CalculateWorldUnlockCost(IEnumerable<int> worldIds, WorldUnlockSheet worldUnlockSheet)
        {
            var cost = 0 * CRYSTAL;

            return worldIds
                .Select(id => worldUnlockSheet.OrderedList.First(r => r.WorldIdToUnlock == id))
                .Aggregate(cost, (current, row) => current + row.CRYSTAL * CRYSTAL);
        }

        public static FungibleAssetValue CalculateBuffGachaCost(int stageId,
            int count,
            CrystalStageBuffGachaSheet stageBuffGachaSheet)
        {
            var cost = CRYSTAL * stageBuffGachaSheet[stageId].CRYSTAL;

            return count == 5 ? cost : cost * 3;
        }

        public static FungibleAssetValue CalculateCrystal(
            Address agentAddress,
            IEnumerable<Equipment> equipmentList,
            FungibleAssetValue balance,
            bool enhancementFailed,
            CrystalEquipmentGrindingSheet crystalEquipmentGrindingSheet,
            CrystalMonsterCollectionMultiplierSheet crystalMonsterCollectionMultiplierSheet,
            StakeRegularRewardSheet stakeRegularRewardSheet
        )
        {
            FungibleAssetValue crystal = 0 * CRYSTAL;
            foreach (var equipment in equipmentList)
            {
                CrystalEquipmentGrindingSheet.Row grindingRow = crystalEquipmentGrindingSheet[equipment.Id];
                crystal += grindingRow.CRYSTAL * CRYSTAL;
                crystal += (BigInteger.Pow(2, equipment.level) - 1) *
                           crystalEquipmentGrindingSheet[grindingRow.EnchantBaseId].CRYSTAL *
                           CRYSTAL;
            }

            // Divide Reward when itemEnhancement failed.
            if (enhancementFailed)
            {
                crystal = crystal.DivRem(2, out _);
            }

            int monsterCollectionLevel = stakeRegularRewardSheet.FindLevelByStakedAmount(agentAddress, balance);

            CrystalMonsterCollectionMultiplierSheet.Row multiplierRow =
                crystalMonsterCollectionMultiplierSheet[monsterCollectionLevel];
            var extra = crystal.DivRem(100, out _) * multiplierRow.Multiplier;
            return crystal + extra;
        }


        public static FungibleAssetValue CalculateMaterialCost(
            int materialId,
            int materialCount,
            CrystalMaterialCostSheet crystalMaterialCostSheet)
        {
            if (!crystalMaterialCostSheet.TryGetValue(materialId, out var costRow))
            {
                throw new ArgumentException($"This material is not replaceable with crystal. id : {materialId}");
            }

            return costRow.CRYSTAL * materialCount * CRYSTAL;
        }

        public static FungibleAssetValue CalculateCombinationCost(FungibleAssetValue crystal,
            CrystalFluctuationSheet.Row row,
            CrystalCostState prevWeeklyCostState = null,
            CrystalCostState beforePrevWeeklyCostState = null)
        {
            if (!(prevWeeklyCostState is null) && !(beforePrevWeeklyCostState is null))
            {
                int multiplier = (int) (prevWeeklyCostState.CRYSTAL.RawValue * 100 /
                                        beforePrevWeeklyCostState.CRYSTAL.RawValue);
                multiplier = Math.Min(row.MaximumRate, Math.Max(row.MinimumRate, multiplier));
                crystal = crystal.DivRem(100, out _) * multiplier;
            }

            return crystal;
        }
    }
}
