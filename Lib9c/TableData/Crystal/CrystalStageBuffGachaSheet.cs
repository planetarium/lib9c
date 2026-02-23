using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Crystal
{
    [Serializable]
    public class CrystalStageBuffGachaSheet : Sheet<int, CrystalStageBuffGachaSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => StageId;
            public int StageId;
            public int MaxStar;
            public int NormalCost;
            public int AdvancedCost;

            public override void Set(IReadOnlyList<string> fields)
            {
                StageId = ParseInt(fields[0]);
                MaxStar = ParseInt(fields[1]);
                NormalCost = ParseInt(fields[2]);
                AdvancedCost = ParseInt(fields[3]);
            }
        }

        public CrystalStageBuffGachaSheet() : base(nameof(CrystalStageBuffGachaSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            base.AddRow(key, value);

            // Extend hard stages as a continuation of normal stages:
            // stage 451..900 duplicates stage 1..450.
            if (key >= 1 && key <= 450)
            {
                var extendedKey = key + 450;
                if (!ContainsKey(extendedKey))
                {
                    base.AddRow(extendedKey, new Row
                    {
                        StageId = extendedKey,
                        MaxStar = value.MaxStar,
                        NormalCost = value.NormalCost,
                        AdvancedCost = value.AdvancedCost,
                    });
                }
            }
        }
    }
}
