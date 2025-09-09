using System;
using System.Collections.Generic;
using System.Linq;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Summon
{
    public class SummonSheet : Sheet<int, SummonSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            /// <summary>
            /// Maximum number of recipes allowed per summon group.
            /// </summary>
            public const int MaxRecipeCount = 100;
            public override int Key => GroupId;

            /// <summary>
            /// Unique identifier for this summon group.
            /// </summary>
            public int GroupId { get; private set; }
            
            /// <summary>
            /// Material ID required for summoning.
            /// </summary>
            public int CostMaterial { get; private set; }
            
            /// <summary>
            /// Quantity of material required for summoning.
            /// </summary>
            public int CostMaterialCount { get; private set; }
            
            /// <summary>
            /// NCG (Nine Chronicles Gold) cost for summoning.
            /// </summary>
            public int CostNcg { get; private set; }

            /// <summary>
            /// Minimum grade to guarantee for 11 summons. Null if not configured.
            /// </summary>
            public int? MinimumGrade11 { get; private set; }
            
            /// <summary>
            /// Number of items to guarantee for 11 summons. Null if not configured.
            /// </summary>
            public int? GuaranteeCount11 { get; private set; }

            /// <summary>
            /// Minimum grade to guarantee for 110 summons. Null if not configured.
            /// </summary>
            public int? MinimumGrade110 { get; private set; }
            
            /// <summary>
            /// Number of items to guarantee for 110 summons. Null if not configured.
            /// </summary>
            public int? GuaranteeCount110 { get; private set; }

            /// <summary>
            /// Determines if grade guarantee is enabled for this summon group.
            /// Returns true if any guarantee settings are configured (either for 11 or 110 summons).
            /// This property is used by summon actions to determine whether to apply grade guarantee logic.
            /// </summary>
            /// <value>True if grade guarantee is configured for this summon group, false otherwise</value>
            public bool UseGradeGuarantee =>
                (MinimumGrade11.HasValue && GuaranteeCount11.HasValue) ||
                (MinimumGrade110.HasValue && GuaranteeCount110.HasValue);

            /// <summary>
            /// List of recipes available for this summon group.
            /// Each tuple contains (recipeId, ratio) where ratio determines the probability of selection.
            /// </summary>
            public readonly List<(int, int)> Recipes = new();

            /// <summary>
            /// Calculates the total ratio of all recipes in this summon group.
            /// </summary>
            /// <returns>Sum of all recipe ratios</returns>
            public int TotalRatio()
            {
                return Recipes.Sum(x => x.Item2);
            }

            /// <summary>
            /// Calculates the cumulative ratio up to the specified recipe index.
            /// Used for weighted random selection of recipes.
            /// </summary>
            /// <param name="index">Recipe index (1-based)</param>
            /// <returns>Cumulative ratio up to the specified index</returns>
            /// <exception cref="IndexOutOfRangeException">Thrown when index is out of valid range</exception>
            public int CumulativeRatio(int index)
            {
                if (index is < 1 or > MaxRecipeCount)
                {
                    throw new IndexOutOfRangeException(
                        $"{index} is not valid index. Use between 1 and {MaxRecipeCount}."
                    );
                }

                var ratio = 0;
                for (var i = 0; i < index; i++)
                {
                    if (i == Recipes.Count) break;
                    ratio += Recipes[i].Item2;
                }

                return ratio;
            }

            /// <summary>
            /// Sets the row data from CSV fields, supporting both legacy and new formats with grade guarantee settings.
            /// Legacy format: groupID,cost_material,cost_material_count,cost_ncg,recipe1ID,recipe1ratio,...
            /// New format: groupID,cost_material,cost_material_count,cost_ncg,GUARANTEE,min_grade_11,count_11,min_grade_110,count_110,recipe1ID,recipe1ratio,...
            /// </summary>
            /// <param name="fields">CSV field values</param>
            public override void Set(IReadOnlyList<string> fields)
            {
                GroupId = ParseInt(fields[0]);
                CostMaterial = ParseInt(fields[1]);
                CostMaterialCount = ParseInt(fields[2]);
                CostNcg = ParseInt(fields[3]);

                // Parse grade guarantee settings if available
                var recipeStartIndex = 4;
                if (fields.Count > 4 && fields[4] == "GUARANTEE")
                {
                    // New format with grade guarantee settings for 11 and 110 summons
                    // Format: GUARANTEE, min_grade_11, count_11, min_grade_110, count_110
                    if (fields.Count >= 9)
                    {
                        // Parse 11 summon settings
                        MinimumGrade11 = !string.IsNullOrEmpty(fields[5]) ? ParseInt(fields[5]) : null;
                        GuaranteeCount11 = !string.IsNullOrEmpty(fields[6]) ? ParseInt(fields[6]) : null;

                        // Parse 110 summon settings
                        MinimumGrade110 = !string.IsNullOrEmpty(fields[7]) ? ParseInt(fields[7]) : null;
                        GuaranteeCount110 = !string.IsNullOrEmpty(fields[8]) ? ParseInt(fields[8]) : null;

                        recipeStartIndex = 9;
                    }
                    else
                    {
                        // Fallback to old format for backward compatibility
                        MinimumGrade11 = !string.IsNullOrEmpty(fields[5]) ? ParseInt(fields[5]) : null;
                        GuaranteeCount11 = !string.IsNullOrEmpty(fields[6]) ? ParseInt(fields[6]) : null;
                        recipeStartIndex = 7;
                    }
                }
                else
                {
                    // Legacy format: groupID,cost_material,cost_material_count,cost_ncg,recipe1ID,recipe1ratio,...
                    // No grade guarantee settings, recipes start at index 4
                    recipeStartIndex = 4;
                }

                // Min. Two recipes are necessary
                Recipes.Add((ParseInt(fields[recipeStartIndex]), ParseInt(fields[recipeStartIndex + 1])));
                Recipes.Add((ParseInt(fields[recipeStartIndex + 2]), ParseInt(fields[recipeStartIndex + 3])));

                // Recipe3 ~ MaxRecipeCount are optional
                var maxRecipeFields = 2 * MaxRecipeCount + recipeStartIndex;
                if (fields.Count > maxRecipeFields)
                {
                    throw new IndexOutOfRangeException(
                        $"Provided recipe count {(fields.Count - recipeStartIndex) / 2} exceeds {MaxRecipeCount}."
                    );
                }

                for (var i = 3; i <= MaxRecipeCount; i++)
                {
                    var idx = 2 * (i - 1) + recipeStartIndex;
                    if (fields.Count >= idx + 2 &&
                        TryParseInt(fields[idx], out _) &&
                        TryParseInt(fields[idx + 1], out _))
                    {
                        Recipes.Add((ParseInt(fields[idx]), ParseInt(fields[idx + 1])));
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public SummonSheet() : base(nameof(SummonSheet))
        {
        }
    }
}
