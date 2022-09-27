using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class HitBuff : StatBuff
    {
        public HitBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
