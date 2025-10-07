using System;
using Lib9c.Model.Skill;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Buff
{
    [Serializable]
    public class Stun : ActionBuff
    {
        public Stun(ActionBuffSheet.Row row) : base(row)
        {
        }

        public Stun(SkillCustomField customField, ActionBuffSheet.Row row) : base(customField, row)
        {
        }

        protected Stun(ActionBuff value) : base(value)
        {
        }

        public override object Clone()
        {
            return new Stun(this);
        }
    }
}
