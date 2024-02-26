using System;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
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
