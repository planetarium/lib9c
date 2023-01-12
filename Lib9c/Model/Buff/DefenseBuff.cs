using System;
using Lib9c.Model.Skill;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Buff
{
    [Serializable]
    public class DefenseBuff : StatBuff
    {
        public DefenseBuff(StatBuffSheet.Row row) : base(row)
        {
        }

        public DefenseBuff(SkillCustomField customField, StatBuffSheet.Row row)
            : base(customField, row)
        {
        }
    }
}
