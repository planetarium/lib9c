using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class CriticalBuff : StatBuff
    {
        public CriticalBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
