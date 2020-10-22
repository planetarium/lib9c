using System;
using System.Collections.Generic;

namespace Nekoyume.TableData
{
    [Serializable]
    public class ArenaConfigSheet : Sheet<string, ArenaConfigSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<string>
        {
            public override string Key => _key;

            private string _key;
            public string Value { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                _key = fields[0];
                Value = fields[1];
            }
        }

        public ArenaConfigSheet() : base(nameof(ArenaConfigSheet))
        {

        }
    }
}
