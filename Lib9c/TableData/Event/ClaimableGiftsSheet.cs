using System.Collections.Generic;
using System.Linq;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.Event
{
    public class ClaimableGiftsSheet : Sheet<int, ClaimableGiftsSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id;
            public long StartedBlockIndex;
            public long EndedBlockIndex;
            public List<(int itemId, int quantity, bool tradable)> Items;

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                StartedBlockIndex = ParseLong(fields[1]);
                EndedBlockIndex = ParseLong(fields[2]);

                Items = new List<(int, int, bool)>();
                for (int i = 0; i < 5; i++)
                {
                    var offset = i * 2;
                    if (!TryParseInt(fields[3 + offset], out var materialId) || materialId == 0 ||
                        !TryParseInt(fields[4 + offset], out var quantity) || quantity == 0)
                    {
                        continue;
                    }

                    var tradable = ParseBool(fields[5 + offset], true);
                    Items.Add((materialId, quantity, tradable));
                }
            }

            public bool Validate(long blockIndex)
            {
                return StartedBlockIndex <= blockIndex && blockIndex <= EndedBlockIndex;
            }
        }

        public ClaimableGiftsSheet() : base(nameof(ClaimableGiftsSheet))
        {
        }

        public bool TryFindRowByBlockIndex(long blockIndex, out Row row)
        {
            row = OrderedList.FirstOrDefault(r => r.StartedBlockIndex <= blockIndex && blockIndex <= r.EndedBlockIndex);
            return row != null;
        }
    }
}
