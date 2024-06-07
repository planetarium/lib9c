using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    public class BuffLinkSheet: Sheet<int, BuffLinkSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => BuffId;
            public int BuffId { get; private set; }
            public int LinkedBuffId { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                BuffId = ParseInt(fields[0]);
                LinkedBuffId = ParseInt(fields[1]);
            }
        }

        public BuffLinkSheet() : base(nameof(BuffLinkSheet))
        {
        }
    }
}
