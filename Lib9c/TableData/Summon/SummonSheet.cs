using System;
using System.Collections.Generic;
using System.Linq;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Summon
{
    public class SummonSheet : Sheet<int, SummonSheet.Row>
    {
        [Serializable]
        public enum ResultType
        {
            Aura,
            RuneStone,
            Grimoire,
            FullCostume,
            Title,
        }

        public class Row : SheetRow<int>
        {
            public const int MaxRecipeCount = 100;
            public override int Key => GroupId;

            public int GroupId { get; private set; }
            public ResultType SummonResultType { get; private set; }
            public int CostMaterial { get; private set; }
            public int CostMaterialCount { get; private set; }
            public int CostNcg { get; private set; }

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
                SummonResultType = (ResultType)Enum.Parse(typeof(ResultType), fields[1]);
                CostMaterial = ParseInt(fields[2]);
                CostMaterialCount = ParseInt(fields[3]);
                CostNcg = ParseInt(fields[4]);
                // Min. Two recipes are necessary
                Recipes.Add((ParseInt(fields[5]), ParseInt(fields[6])));
                Recipes.Add((ParseInt(fields[7]), ParseInt(fields[8])));

                // Recipe3 ~ 30 are optional
                if (fields.Count > 2 * MaxRecipeCount + 5)
                {
                    throw new IndexOutOfRangeException(
                        $"Provided recipe count {(fields.Count - 4) / 2} exceeds {MaxRecipeCount}."
                    );
                }

                for (var i = 3; i <= MaxRecipeCount; i++)
                {
                    var idx = 2 * i + 3;
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
