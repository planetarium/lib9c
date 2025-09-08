using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.TableData.Summon;

namespace Nekoyume.Helper
{
    public static class SummonHelper
    {
        public static readonly int[] AllowedSummonCount = {1, 10, 100};

        /// <summary>
        /// summon에서 쓸 count를 넣으면 허용된 count인지 체크한 뒤 허용 여부를 반환합니다.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public static bool CheckSummonCountIsValid(int count)
        {
            return AllowedSummonCount.Contains(count);
        }

        /// <summary>
        /// count를 넣으면 10+1 이라는 규칙에 의해 증가된 값을 반환합니다.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public static int CalculateSummonCount(int count)
        {
            return count + count / 10;
        }

        public static int GetSummonRecipeIdByRandom(SummonSheet.Row summonRow,
            IRandom random)
        {
            var targetRatio = random.Next(1, summonRow.TotalRatio() + 1);
            for (var j = 1; j <= SummonSheet.Row.MaxRecipeCount; j++)
            {
                if (targetRatio <= summonRow.CumulativeRatio(j))
                {
                    return summonRow.Recipes[j - 1].Item1;
                }
            }

            return summonRow.Recipes.First().Item1;
        }

        /// <summary>
        /// Determines guarantee settings based on summon count and SummonSheet.Row configuration.
        /// </summary>
        /// <param name="summonRow">The summon row containing guarantee settings</param>
        /// <param name="summonCount">The number of summons</param>
        /// <returns>A tuple containing (useGuarantee, minimumGrade, guaranteeCount)</returns>
        public static (bool useGuarantee, int minimumGrade, int guaranteeCount) GetGuaranteeSettings(
            SummonSheet.Row summonRow,
            int summonCount)
        {
            if (summonCount >= 110)
            {
                // Use 110 summon guarantee settings
                var useGuarantee = summonRow.MinimumGrade110.HasValue && summonRow.GuaranteeCount110.HasValue;
                var minimumGrade = summonRow.MinimumGrade110 ?? 0;
                var guaranteeCount = summonRow.GuaranteeCount110 ?? 0;
                return (useGuarantee, minimumGrade, guaranteeCount);
            }
            else if (summonCount >= 11)
            {
                // Use 11 summon guarantee settings
                var useGuarantee = summonRow.MinimumGrade11.HasValue && summonRow.GuaranteeCount11.HasValue;
                var minimumGrade = summonRow.MinimumGrade11 ?? 0;
                var guaranteeCount = summonRow.GuaranteeCount11 ?? 0;
                return (useGuarantee, minimumGrade, guaranteeCount);
            }
            else
            {
                // No guarantee for less than 11 summons
                return (false, 0, 0);
            }
        }

        /// <summary>
        /// Gets summon recipe IDs with grade guarantee system based on summon count.
        /// Applies different guarantee settings for 11 summons vs 110 summons.
        /// This method processes items one by one to maintain the same random call order as the original implementation.
        /// Uses grade guarantee settings from SummonSheet.Row.
        /// </summary>
        /// <param name="summonRow">Summon sheet row containing recipes, ratios, and grade guarantee settings</param>
        /// <param name="summonCount">Number of items to summon</param>
        /// <param name="random">Random number generator</param>
        /// <param name="equipmentItemSheet">Equipment item sheet to check grades</param>
        /// <param name="equipmentItemRecipeSheet">Equipment item recipe sheet to map recipe IDs to equipment IDs</param>
        /// <returns>List of recipe IDs with grade guarantee applied</returns>
        public static List<int> GetSummonRecipeIdsWithGradeGuarantee(
            SummonSheet.Row summonRow,
            int summonCount,
            IRandom random,
            EquipmentItemSheet equipmentItemSheet,
            EquipmentItemRecipeSheet equipmentItemRecipeSheet)
        {
            var result = new List<int>();
            var guaranteedCount = 0;

            // Get guarantee settings
            var (useGuarantee, minimumGrade, guaranteeCount) = GetGuaranteeSettings(summonRow, summonCount);

            // Process each item one by one to maintain random call order
            for (var i = 0; i < summonCount; i++)
            {
                int recipeId;

                // Check if we need grade guarantee
                // For 110 summons, we need to guarantee multiple items, so we check remaining summons
                var needsGuarantee = useGuarantee &&
                                   guaranteedCount < guaranteeCount &&
                                   (summonCount - i) <= (guaranteeCount - guaranteedCount);

                if (needsGuarantee)
                {
                    // Select item from minimum grade or higher
                    recipeId = GetGuaranteedGradeRecipeId(summonRow, random, equipmentItemSheet, equipmentItemRecipeSheet, minimumGrade);
                    guaranteedCount++;
                }
                else
                {
                    // Select item normally
                    recipeId = GetSummonRecipeIdByRandom(summonRow, random);
                }

                result.Add(recipeId);
            }

            return result;
        }

        /// <summary>
        /// Gets summon recipe IDs with grade guarantee system for costumes.
        /// Applies different guarantee settings for 11 summons vs 110 summons.
        /// This method processes items one by one to maintain the same random call order as the original implementation.
        /// Uses grade guarantee settings from SummonSheet.Row.
        /// </summary>
        /// <param name="summonRow">Summon sheet row containing recipes, ratios, and grade guarantee settings</param>
        /// <param name="summonCount">Number of items to summon</param>
        /// <param name="random">Random number generator</param>
        /// <param name="costumeItemSheet">Costume item sheet to check grades</param>
        /// <param name="equipmentItemRecipeSheet">Equipment item recipe sheet (not used for costumes, can be null)</param>
        /// <returns>List of recipe IDs with grade guarantee applied</returns>
        public static List<int> GetSummonRecipeIdsWithGradeGuarantee(
            SummonSheet.Row summonRow,
            int summonCount,
            IRandom random,
            CostumeItemSheet costumeItemSheet,
            EquipmentItemRecipeSheet equipmentItemRecipeSheet)
        {
            var result = new List<int>();
            var guaranteedCount = 0;

            // Get guarantee settings
            var (useGuarantee, minimumGrade, guaranteeCount) = GetGuaranteeSettings(summonRow, summonCount);

            // Process each item one by one to maintain random call order
            for (var i = 0; i < summonCount; i++)
            {
                int recipeId;

                // Check if we need grade guarantee
                // For 110 summons, we need to guarantee multiple items, so we check remaining summons
                var needsGuarantee = useGuarantee &&
                                   guaranteedCount < guaranteeCount &&
                                   (summonCount - i) <= (guaranteeCount - guaranteedCount);

                if (needsGuarantee)
                {
                    // Select item from minimum grade or higher
                    recipeId = GetGuaranteedGradeRecipeIdForCostume(summonRow, random, costumeItemSheet, minimumGrade);
                    guaranteedCount++;
                }
                else
                {
                    // Select item normally
                    recipeId = GetSummonRecipeIdByRandom(summonRow, random);
                }

                result.Add(recipeId);
            }

            return result;
        }

        /// <summary>
        /// Gets a recipe ID that guarantees minimum grade or higher for costumes.
        /// </summary>
        /// <param name="summonRow">Summon sheet row</param>
        /// <param name="random">Random number generator</param>
        /// <param name="costumeItemSheet">Costume item sheet</param>
        /// <param name="minimumGrade">Minimum grade to guarantee</param>
        /// <returns>Recipe ID that meets minimum grade requirement</returns>
        private static int GetGuaranteedGradeRecipeIdForCostume(
            SummonSheet.Row summonRow,
            IRandom random,
            CostumeItemSheet costumeItemSheet,
            int minimumGrade)
        {
            // Filter recipes that meet minimum grade requirement
            var eligibleRecipes = new List<(int recipeId, int ratio)>();
            var totalEligibleRatio = 0;

            foreach (var (recipeId, ratio) in summonRow.Recipes)
            {
                if (TryGetCostumeGradeFromRecipeId(recipeId, costumeItemSheet, out var grade) && grade >= minimumGrade)
                {
                    eligibleRecipes.Add((recipeId, ratio));
                    totalEligibleRatio += ratio;
                }
            }

            // If no eligible recipes found, return the first recipe (fallback)
            if (eligibleRecipes.Count == 0)
            {
                return summonRow.Recipes.First().Item1;
            }

            // Select from eligible recipes based on their ratios
            var targetRatio = random.Next(1, totalEligibleRatio + 1);
            var cumulativeRatio = 0;

            foreach (var (recipeId, ratio) in eligibleRecipes)
            {
                cumulativeRatio += ratio;
                if (targetRatio <= cumulativeRatio)
                {
                    return recipeId;
                }
            }

            return eligibleRecipes.First().recipeId;
        }

        /// <summary>
        /// Tries to get the grade of a costume from its recipe ID.
        /// </summary>
        /// <param name="recipeId">Recipe ID (which is the costume ID for costumes)</param>
        /// <param name="costumeItemSheet">Costume item sheet</param>
        /// <param name="grade">Output grade if found</param>
        /// <returns>True if grade was found</returns>
        private static bool TryGetCostumeGradeFromRecipeId(
            int recipeId,
            CostumeItemSheet costumeItemSheet,
            out int grade)
        {
            grade = 0;

            // For costumes, the recipe ID is the costume ID
            if (!costumeItemSheet.TryGetValue(recipeId, out var costumeRow))
            {
                return false;
            }

            // Get the grade from the costume
            grade = costumeRow.Grade;
            return true;
        }

        /// <summary>
        /// Gets a recipe ID that guarantees minimum grade or higher.
        /// </summary>
        /// <param name="summonRow">Summon sheet row</param>
        /// <param name="random">Random number generator</param>
        /// <param name="equipmentItemSheet">Equipment item sheet</param>
        /// <param name="equipmentItemRecipeSheet">Equipment item recipe sheet</param>
        /// <param name="minimumGrade">Minimum grade to guarantee</param>
        /// <returns>Recipe ID that meets minimum grade requirement</returns>
        private static int GetGuaranteedGradeRecipeId(
            SummonSheet.Row summonRow,
            IRandom random,
            EquipmentItemSheet equipmentItemSheet,
            EquipmentItemRecipeSheet equipmentItemRecipeSheet,
            int minimumGrade)
        {
            // Filter recipes that meet minimum grade requirement
            var eligibleRecipes = new List<(int recipeId, int ratio)>();
            var totalEligibleRatio = 0;

            foreach (var (recipeId, ratio) in summonRow.Recipes)
            {
                if (TryGetItemGradeFromRecipeId(recipeId, equipmentItemSheet, equipmentItemRecipeSheet, out var grade) && grade >= minimumGrade)
                {
                    eligibleRecipes.Add((recipeId, ratio));
                    totalEligibleRatio += ratio;
                }
            }

            // If no eligible recipes found, return the first recipe (fallback)
            if (eligibleRecipes.Count == 0)
            {
                return summonRow.Recipes.First().Item1;
            }

            // Select from eligible recipes based on their ratios
            var targetRatio = random.Next(1, totalEligibleRatio + 1);
            var cumulativeRatio = 0;

            foreach (var (recipeId, ratio) in eligibleRecipes)
            {
                cumulativeRatio += ratio;
                if (targetRatio <= cumulativeRatio)
                {
                    return recipeId;
                }
            }

            return eligibleRecipes.First().recipeId;
        }

        /// <summary>
        /// Tries to get the grade of an item from its recipe ID.
        /// </summary>
        /// <param name="recipeId">Recipe ID</param>
        /// <param name="equipmentItemSheet">Equipment item sheet</param>
        /// <param name="equipmentItemRecipeSheet">Equipment item recipe sheet</param>
        /// <param name="grade">Output grade if found</param>
        /// <returns>True if grade was found</returns>
        private static bool TryGetItemGradeFromRecipeId(
            int recipeId,
            EquipmentItemSheet equipmentItemSheet,
            EquipmentItemRecipeSheet equipmentItemRecipeSheet,
            out int grade)
        {
            grade = 0;

            // 1. Look up the recipe in EquipmentItemRecipeSheet
            if (!equipmentItemRecipeSheet.TryGetValue(recipeId, out var recipeRow))
            {
                return false;
            }

            // 2. Get the result equipment ID from the recipe
            var resultEquipmentId = recipeRow.ResultEquipmentId;

            // 3. Look up the equipment in EquipmentItemSheet
            if (!equipmentItemSheet.TryGetValue(resultEquipmentId, out var equipmentRow))
            {
                return false;
            }

            // 4. Get the grade from the equipment
            grade = equipmentRow.Grade;
            return true;
        }
    }
}
