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

        protected IceShield(IceShield value) : base(value)
        {
        }

        public override object Clone()
        {
            return new IceShield(this);
        }

        public StatBuff FrostBite(StatBuffSheet statBuffSheet)
        {
            var row = statBuffSheet[801000];
            var frostBite = BuffFactory.GetStatBuff(row);
            return frostBite;
        }
    }
}
