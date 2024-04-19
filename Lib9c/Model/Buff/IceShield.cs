using System;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class IceShield : ActionBuff
    {
        public IceShield(ActionBuffSheet.Row row) : base(row)
        {
        }

        public IceShield(SkillCustomField customField, ActionBuffSheet.Row row) : base(customField, row)
        {
        }

        public IceShield(ActionBuff value) : base(value)
        {
        }

        public override object Clone()
        {
            return new IceShield(this);
        }
    }
}
