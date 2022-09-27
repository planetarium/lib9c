using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class SpeedBuff : StatBuff
    {
        public SpeedBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
