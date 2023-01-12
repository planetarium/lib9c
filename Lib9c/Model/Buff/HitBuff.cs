using System;
using Lib9c.Model.Skill;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Buff
{
    [Serializable]
    public class HitBuff : StatBuff
    {
        public HitBuff(StatBuffSheet.Row row) : base(row)
        {
        }

        public HitBuff(SkillCustomField customField, StatBuffSheet.Row row)
            : base(customField, row)
        {
        }
    }
}
