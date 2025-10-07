using System;
using Lib9c.Model.Skill;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Buff
{
    [Serializable]
    public class Dispel : ActionBuff
    {
        public Dispel(ActionBuffSheet.Row row) : base(row)
        {
        }

        public Dispel(SkillCustomField customField, ActionBuffSheet.Row row)
            : base(customField, row)
        {
        }

        protected Dispel(ActionBuff value) : base(value)
        {
        }

        public override object Clone()
        {
            return new Dispel(this);
        }
    }
}
