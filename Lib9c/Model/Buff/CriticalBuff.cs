using System;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class CriticalBuff : Buff
    {
        public CriticalBuff(BuffSheet.Row row) : base(row)
        {
        }

        public CriticalBuff(Buff buff) : base(buff)
        {
        }
        
        public CriticalBuff(Dictionary serialized) : base(serialized)
        {
        }

        public CriticalBuff(IValue serialized) : this((Dictionary) serialized)
        {
        }
    }
}
