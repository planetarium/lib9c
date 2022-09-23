using System;
using Nekoyume.TableData;

#nullable disable
namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class HPBuff : StatBuff
    {
        public HPBuff(StatBuffSheet.Row row) : base(row)
        {
        }
    }
}
