using System;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class SpeedBuff : Buff
    {
        public SpeedBuff(BuffSheet.Row row) : base(row)
        {
        }

        public SpeedBuff(Buff buff) : base(buff)
        {
        }

        public SpeedBuff(Dictionary serialized) : base(serialized)
        {
        }

        public SpeedBuff(IValue serialized) : this((Dictionary) serialized)
        {
        }
    }
}
