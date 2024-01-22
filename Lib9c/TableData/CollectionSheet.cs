using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    public class CollectionSheet : Sheet<int, CollectionSheet.Row>
    {

        public class CollectionMaterial
        {
            public int ItemId;
            public int Count;
            public int Level;
            public int OptionCount;
            public bool SkillContains;

            public bool Validate(ItemUsable itemUsable)
            {
                switch (itemUsable)
                {
                    case Equipment equipment:
                        return equipment.Id == ItemId && equipment.level == Level &&
                               equipment.GetOptionCount() == OptionCount &&
                               (equipment.Skills.Any() == SkillContains || equipment.BuffSkills.Any() == SkillContains);
                    case Consumable consumable:
                        return consumable.Id == ItemId;
                    default:
                        return false;
                }
            }
        }

        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public int Id { get; private set; }

            public List<CollectionMaterial> Materials = new();

            public List<StatModifier> StatModifiers = new();
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                for (int i = 0; i < 6; i++)
                {
                    var offset = i * 5;
                    if (!TryParseInt(fields[1 + offset], out var itemId) || itemId == 0)
                    {
                        continue;
                    }
                    Materials.Add(new CollectionMaterial
                    {
                        ItemId = itemId,
                        Count = ParseInt(fields[2 + offset]),
                        Level = ParseInt(fields[3 + offset], 0),
                        OptionCount = ParseInt(fields[4 + offset], 0),
                        SkillContains = ParseBool(fields[5 + offset], false)
                    });
                }

                for (int i = 0; i < 3; i++)
                {
                    var offset = i * 3;
                    var statType = fields[28 + offset];
                    if (string.IsNullOrEmpty(statType))
                    {
                        continue;
                    }
                    StatModifiers.Add(new StatModifier(
                        (StatType) Enum.Parse(typeof(StatType), statType),
                        (StatModifier.OperationType) Enum.Parse(typeof(StatModifier.OperationType), fields[29 + offset]),
                        ParseInt(fields[30 + offset])));
                }
            }
        }

        public CollectionSheet() : base(nameof(CollectionSheet))
        {
        }
    }
}
