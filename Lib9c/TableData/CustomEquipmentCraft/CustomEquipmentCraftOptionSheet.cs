using System;
using System.Collections.Generic;
using Lib9c.Model.Item;
using Lib9c.Model.Stat;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.CustomEquipmentCraft
{
    [Serializable]
    public class CustomEquipmentCraftOptionSheet
        : Sheet<int, CustomEquipmentCraftOptionSheet.Row>
    {
        public struct SubStat
        {
            public StatType StatType;
            public int Ratio;
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            private readonly List<StatType> StatTypes = new List<StatType>
            {
                StatType.HP, StatType.ATK, StatType.DEF, StatType.CRI, StatType.HIT, StatType.SPD,
                StatType.DRV, StatType.DRR, StatType.CDMG, StatType.ArmorPenetration,
                StatType.Thorn,
            };


            public override int Key => Id;

            public int Id { get; private set; }
            public ItemSubType ItemSubType { get; private set; }
            public int Ratio { get; private set; }

            public int TotalOptionRatio { get; private set; }

            public List<SubStat> SubStatData { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                ItemSubType = (ItemSubType)Enum.Parse(typeof(ItemSubType), fields[1]);
                Ratio = ParseInt(fields[2]);
                SubStatData = new List<SubStat>();

                var idx = 3;
                foreach (var st in StatTypes)
                {
                    if (TryParseInt(fields[idx], out var ratio))
                    {
                        SubStatData.Add(new SubStat { StatType = st, Ratio = ratio });
                        TotalOptionRatio += ratio;
                    }

                    idx++;
                }
            }
        }

        public CustomEquipmentCraftOptionSheet() : base(nameof(CustomEquipmentCraftOptionSheet))
        {
        }
    }
}
