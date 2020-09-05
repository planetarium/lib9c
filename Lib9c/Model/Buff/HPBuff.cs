using System;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class HPBuff : Buff
    {
        public HPBuff(BuffSheet.Row row) : base(row)
        {
        }

        public HPBuff(Buff buff) : base(buff)
        {
        }

        public HPBuff(Dictionary serialized) : base(serialized)
        {
        }

        public HPBuff(IValue serialized) : this((Dictionary) serialized)
        {
        }
    }
}
