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
            public const int MaxRecipeCount = 100;
            public override int Key => GroupId;

            public int GroupId { get; private set; }
            public int CostMaterial { get; private set; }
            public int CostMaterialCount { get; private set; }
            public int CostNcg { get; private set; }

            // Grade guarantee settings for 11 summons
            public int? MinimumGrade11 { get; private set; }
            public int? GuaranteeCount11 { get; private set; }

            // Grade guarantee settings for 110 summons
            public int? MinimumGrade110 { get; private set; }
            public int? GuaranteeCount110 { get; private set; }

            /// <summary>
            /// Determines if grade guarantee is enabled for this summon group.
            /// Returns true if any guarantee settings are configured (either for 11 or 110 summons).
            /// </summary>
            public bool UseGradeGuarantee =>
                (MinimumGrade11.HasValue && GuaranteeCount11.HasValue) ||
                (MinimumGrade110.HasValue && GuaranteeCount110.HasValue);

            public readonly List<(int, int)> Recipes = new();

            public int TotalRatio()
            {
                return Recipes.Sum(x => x.Item2);
            }

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
