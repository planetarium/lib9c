#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.TableData.CustomEquipmentCraft;
using static System.Numerics.BigInteger;

namespace Nekoyume.Helper
{
    public static class CustomCraftHelper
    {
        public static int SelectCp(
            CustomEquipmentCraftRelationshipSheet.Row relationshipRow,
            IRandom random
        )
        {
            var selector =
                new WeightedSelector<CustomEquipmentCraftRelationshipSheet.CpGroup>(random);
            foreach (var group in relationshipRow.CpGroups)
            {
                selector.Add(group, group.Ratio);
            }

            return selector.Select(1).First().SelectCp(random);
        }

        public static (BigInteger, IDictionary<int, int>)? CalculateAdditionalCost(
            int relationship,
            CustomEquipmentCraftRelationshipSheet relationshipSheet
        )
        {
            var targetRow = relationshipSheet.OrderedList!.FirstOrDefault(
                row => row.Relationship == relationship + 1
            );

            if (targetRow is null)
            {
                return null;
            }

            var ncgCost = targetRow.GoldAmount;
            var itemCosts = new Dictionary<int, int>();
            foreach (var itemCost in targetRow.MaterialCosts)
            {
                itemCosts[itemCost.ItemId] = itemCost.Amount;
            }

            return (ncgCost, itemCosts);
        }

        public static (BigInteger, IDictionary<int, int>) CalculateCraftCost(
            int iconId,
            int relationship,
            MaterialItemSheet materialItemSheet,
            CustomEquipmentCraftRecipeSheet.Row recipeRow,
            CustomEquipmentCraftRelationshipSheet.Row relationshipRow,
            decimal iconCostMultiplier
        )
        {
            var ncgCost = Zero;
            var itemCosts = new Dictionary<int, int>();
            var scrollItemId = materialItemSheet.OrderedList!
                .First(row => row.ItemSubType == ItemSubType.Scroll).Id;
            var circleItemId = materialItemSheet.OrderedList!
                .First(row => row.ItemSubType == ItemSubType.Circle).Id;

            // Scroll cost : {recipe.scroll} * {relationship multiplier}
            itemCosts[scrollItemId] =
                (int)Math.Floor(recipeRow.ScrollAmount * relationshipRow.CostMultiplier / 10000m);

            // Circle cost : {recipe.circle} * {relationship multiplier} * {Random multiplier}
            var circleCost =
                (decimal)recipeRow.CircleAmount * relationshipRow.CostMultiplier / 10000m;
            if (iconId != 0)
            {
                circleCost = circleCost * iconCostMultiplier / 10000m;
            }

            if (relationshipRow.Relationship == relationship)
            {
            }

            itemCosts[circleItemId] = (int)Math.Floor(circleCost);

            return (ncgCost, itemCosts);
        }
    }
}
