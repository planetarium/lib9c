using System.Collections.Generic;

namespace Lib9c.TableData.Item
{
    using static TableExtensions;

    public class ItemRequirementSheet : Sheet<int, ItemRequirementSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => ItemId;
            public int ItemId { get; private set; }
            public int Level { get; private set; }
            public int MimisLevel { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                ItemId = ParseInt(fields[0]);
                Level = GameConfig.IsEditor ? 1 : ParseInt(fields[1]);
                MimisLevel = GameConfig.IsEditor ? 1 : ParseInt(fields[2]);
            }
        }

        public ItemRequirementSheet() : base(nameof(ItemRequirementSheet))
        {
        }
    }
}
