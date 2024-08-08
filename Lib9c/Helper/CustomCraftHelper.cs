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
            MaterialItemSheet materialItemSheet,
            CustomEquipmentCraftRecipeSheet.Row recipeRow,
            CustomEquipmentCraftRelationshipSheet.Row relationshipRow,
            CustomEquipmentCraftCostSheet.Row? costRow,
            decimal iconCostMultiplier
        )
        {
            var ncgCost = BigInteger.Zero;
            var itemCosts = new Dictionary<int, int>();
            var drawingItemId = materialItemSheet.OrderedList!
                .First(row => row.ItemSubType == ItemSubType.Drawing).Id;
            var drawingToolItemId = materialItemSheet.OrderedList!
                .First(row => row.ItemSubType == ItemSubType.DrawingTool).Id;

            itemCosts[drawingItemId] =
                (int)Math.Floor(recipeRow.DrawingAmount * relationshipRow.CostMultiplier / 10000m);
            var drawingToolCost =
                (decimal)recipeRow.DrawingToolAmount * relationshipRow.CostMultiplier / 10000m;
            if (iconId != 0)
            {
                drawingToolCost = drawingToolCost * iconCostMultiplier / 10000m;
            }

            if (costRow is not null)
            {
                ncgCost = costRow.GoldAmount;
                foreach (var itemCost in costRow.MaterialCosts)
                {
                    itemCosts[itemCost.ItemId] = itemCost.Amount;
                }
            }

            itemCosts[drawingToolItemId] = (int)Math.Floor(drawingToolCost);

            return (ncgCost, itemCosts.ToImmutableSortedDictionary());
        }
    }
}
