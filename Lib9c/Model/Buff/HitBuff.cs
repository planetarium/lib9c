using System;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class HitBuff : Buff
    {
        public HitBuff(BuffSheet.Row row) : base(row)
        {
        }

        public HitBuff(Buff buff) : base(buff)
        {
        }

        public HitBuff(Dictionary serialized) : base(serialized)
        {
        }

        public HitBuff(IValue serialized) : this((Dictionary) serialized)
        {
        }
    }
}
