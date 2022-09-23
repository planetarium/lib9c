using System;
using Nekoyume.TableData;

#nullable disable
namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class HitBuff : StatBuff
    {
        public HitBuff(StatBuffSheet.Row row) : base(row)
        {
        }
    }
}
