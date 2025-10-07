using System;
using Lib9c.Model.Skill;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Buff
{
    [Serializable]
    public class Focus : ActionBuff
    {
        public Focus(ActionBuffSheet.Row row) : base(row)
        {
        }

        public Focus(SkillCustomField customField, ActionBuffSheet.Row row) :
            base(customField, row)
        {
        }

        protected Focus(Focus value) : base(value)
        {
        }

        public override object Clone()
        {
            return new Focus(this);
        }
    }
}
