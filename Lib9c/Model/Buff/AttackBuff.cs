using System;
using Lib9c.Model.Skill;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Buff
{
    [Serializable]
    public class AttackBuff : StatBuff
    {
        public AttackBuff(StatBuffSheet.Row row) : base(row)
        {
        }

        public AttackBuff(SkillCustomField customField, StatBuffSheet.Row row)
            : base(customField, row)
        {
        }
    }
}
