using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    public class CollectionSheet : Sheet<int, CollectionSheet.Row>
    {

        public class CollectionMaterial
        {
            public int ItemId;
            public int Level;
            public int Count;
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
                    var offset = i * 3;
                    if (!TryParseInt(fields[1 + offset], out var itemId) || itemId == 0)
                    {
                        continue;
                    }
                    Materials.Add(new CollectionMaterial
                    {
                        ItemId = itemId,
                        Level = ParseInt(fields[2 + offset]),
                        Count = ParseInt(fields[3 + offset])
                    });
                }

                for (int i = 0; i < 3; i++)
                {
                    var offset = i * 3;
                    var statType = fields[19 + offset];
                    if (string.IsNullOrEmpty(statType))
                    {
                        continue;
                    }
                    StatModifiers.Add(new StatModifier(
                        (StatType) Enum.Parse(typeof(StatType), statType),
                        (StatModifier.OperationType) Enum.Parse(typeof(StatModifier.OperationType), fields[20 + offset]),
                        ParseInt(fields[21 + offset])));
                }
            }
        }

        public CollectionSheet() : base(nameof(CollectionSheet))
        {
        }
    }
}
