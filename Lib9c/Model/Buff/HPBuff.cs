using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class HPBuff : StatBuff
    {
        public HPBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
