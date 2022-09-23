using System;
using Nekoyume.Model;
using Nekoyume.TableData;

#nullable disable
namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class DefenseBuff : StatBuff
    {
        public DefenseBuff(StatBuffSheet.Row row) : base(row)
        {
        }
    }
}
