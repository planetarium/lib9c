using System;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class DefenseBuff : Buff
    {
        public DefenseBuff(BuffSheet.Row row) : base(row)
        {
        }

        public DefenseBuff(Buff buff) : base(buff)
        {
        }

        public DefenseBuff(Dictionary serialized) : base(serialized)
        {
        }

        public DefenseBuff(IValue serialized) : this((Dictionary) serialized)
        {
        }
    }
}
