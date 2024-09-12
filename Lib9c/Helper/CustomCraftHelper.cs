#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.TableData.CustomEquipmentCraft;

namespace Nekoyume.Helper
{
    public static class CustomCraftHelper
    {
        public static (BigInteger, IDictionary<int, int>) CalculateCraftCost(
            int iconId,
            int relationship,
            MaterialItemSheet materialItemSheet,
            CustomEquipmentCraftRecipeSheet.Row recipeRow,
            CustomEquipmentCraftRelationshipSheet.Row relationshipRow,
            decimal iconCostMultiplier
        )
        {
            var ncgCost = BigInteger.Zero;
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
                ncgCost = relationshipRow.GoldAmount;
                foreach (var itemCost in relationshipRow.MaterialCosts)
                {
                    itemCosts[itemCost.ItemId] = itemCost.Amount;
                }
            }

            itemCosts[circleItemId] = (int)Math.Floor(circleCost);

            return (ncgCost, itemCosts.ToImmutableSortedDictionary());
        }
    }
}
