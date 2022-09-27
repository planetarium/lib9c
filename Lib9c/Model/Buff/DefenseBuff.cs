using System;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class DefenseBuff : StatBuff
    {
        public DefenseBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
