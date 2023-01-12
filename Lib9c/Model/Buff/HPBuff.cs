using System;
using Lib9c.Model.Skill;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Buff
{
    [Serializable]
    public class HPBuff : StatBuff
    {
        public HPBuff(StatBuffSheet.Row row) : base(row)
        {
        }

        public HPBuff(SkillCustomField customField, StatBuffSheet.Row row)
            : base(customField, row)
        {
        }
    }
}
