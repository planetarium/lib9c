using System;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
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
