using System;
using Lib9c.Model.Skill;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Buff
{
    [Serializable]
    public class SpeedBuff : StatBuff
    {
        public SpeedBuff(StatBuffSheet.Row row) : base(row)
        {
        }

        public SpeedBuff(SkillCustomField customField, StatBuffSheet.Row row)
            : base(customField, row)
        {
        }
    }
}
