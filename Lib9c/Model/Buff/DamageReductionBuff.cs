using System;
using Lib9c.Model.Skill;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Buff
{
    [Serializable]
    public class DamageReductionBuff : StatBuff
    {
        public DamageReductionBuff(StatBuffSheet.Row row) : base(row)
        {
        }

        public DamageReductionBuff(SkillCustomField customField, StatBuffSheet.Row row)
            : base(customField, row)
        {
        }
    }
}
