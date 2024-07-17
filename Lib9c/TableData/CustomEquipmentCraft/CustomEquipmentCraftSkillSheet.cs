using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.CustomEquipmentCraft
{
    [Serializable]
    public class CustomEquipmentCraftSkillSheet : Sheet<int, CustomEquipmentCraftSkillSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public int Id { get; private set; }
            public int SkillId { get; private set; }
            public int SkillDamageMin { get; private set; }
            public int SkillDamageMax { get; private set; }
            public int SkillChanceMin { get; private set; }
            public int SkillChanceMax { get; private set; }
            public int StatDamageRatioMin { get; private set; }
            public int StatDamageRatioMax { get; private set; }
            public StatType ReferencedStatType { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
            }
        }

        public CustomEquipmentCraftSkillSheet() : base(nameof(CustomEquipmentCraftSkillSheet))
        {
        }
    }
}
