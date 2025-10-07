using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Action;
using Lib9c.Model.Collection;
using Lib9c.Model.Item;
using Lib9c.Model.Stat;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData
{
    public class CollectionSheet : Sheet<int, CollectionSheet.Row>
    {
        public class RequiredMaterial
        {
            public int ItemId;
            public int Count;
            public int Level;
            public bool SkillContains;

            private bool Validate(Equipment equipment)
            {
                return equipment.Id == ItemId && equipment.level == Level && CheckSkill(equipment);
            }

            /// <summary>
            /// Checks if the given equipment has skills or buff skills based on the RequiredMaterial configuration.
            /// </summary>
            /// <param name="equipment">The equipment to check.</param>
            /// <returns>True if the equipment has skills or buff skills when SkillContains. otherwise equipment has no skills and buff skills.</returns>
            private bool CheckSkill(Equipment equipment)
            {
                if (SkillContains)
                {
                    return equipment.Skills.Any() || equipment.BuffSkills.Any();
                }

                return !equipment.Skills.Any() && !equipment.BuffSkills.Any();
            }

            private bool Validate(Costume costume)
            {
                return costume.Id == ItemId;
            }

            /// <summary>
            /// Retrieves the <see cref="ICollectionMaterial"/> object from the given collection of materials based on the item ID and count.
            /// </summary>
            /// <param name="materials">The collection of materials to search.</param>
            /// <returns>The <see cref="ICollectionMaterial"/> object if found; otherwise, an exception is thrown.</returns>
            public ICollectionMaterial GetMaterial(IEnumerable<ICollectionMaterial> materials)
            {
                var material = materials.FirstOrDefault(m =>
                    m.ItemId == ItemId && m.ItemCount == Count);
                if (material is null)
                {
                    throw new InvalidMaterialException(
                        $"can't find material {ItemId}/{Count}");
                }

                return material;
            }

            public bool Validate(INonFungibleItem nonFungibleItem)
            {
                switch (nonFungibleItem)
                {
                    case Costume costume:
                        return Validate(costume);
                    case Equipment equipment:
                        return Validate(equipment);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(nonFungibleItem));
                }
            }
        }

        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public int Id { get; private set; }

            public List<RequiredMaterial> Materials = new();

            public List<StatModifier> StatModifiers = new();
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                for (int i = 0; i < 6; i++)
                {
                    var offset = i * 4;
                    if (!TryParseInt(fields[1 + offset], out var itemId) || itemId == 0)
                    {
                        continue;
                    }
                    Materials.Add(new RequiredMaterial
                    {
                        ItemId = itemId,
                        Count = ParseInt(fields[2 + offset]),
                        Level = ParseInt(fields[3 + offset], 0),
                        SkillContains = ParseBool(fields[4 + offset], false)
                    });
                }

                for (int i = 0; i < 3; i++)
                {
                    var offset = i * 3;
                    var statType = fields[25 + offset];
                    if (string.IsNullOrEmpty(statType))
                    {
                        continue;
                    }
                    StatModifiers.Add(new StatModifier(
                        (StatType) Enum.Parse(typeof(StatType), statType),
                        (StatModifier.OperationType) Enum.Parse(typeof(StatModifier.OperationType), fields[26 + offset]),
                        ParseInt(fields[27 + offset])));
                }
            }
        }

        public CollectionSheet() : base(nameof(CollectionSheet))
        {
        }
    }
}
