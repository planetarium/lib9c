using System;
using Lib9c.Model.Skill;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Buff
{
    [Serializable]
    public class CriticalBuff : StatBuff
    {
        public CriticalBuff(StatBuffSheet.Row row) : base(row)
        {
        }

        public CriticalBuff(SkillCustomField customField, StatBuffSheet.Row row)
            : base(customField, row)
        {
        }
    }
}
