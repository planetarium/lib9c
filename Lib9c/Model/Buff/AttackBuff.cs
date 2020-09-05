using System;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class AttackBuff : Buff
    {
        public AttackBuff(BuffSheet.Row row) : base(row)
        {
        }

        public AttackBuff(Buff buff) : base(buff)
        {
        }

        public AttackBuff(Dictionary serialized) : base(serialized)
        {
        }

        public AttackBuff(IValue serialized) : this((Dictionary) serialized)
        {
        }
    }
}
