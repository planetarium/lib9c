using System;
using Lib9c.Model.Skill;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Buff
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

        public StatBuff FrostBite(StatBuffSheet statBuffSheet, BuffLinkSheet buffLinkSheet)
        {
            var row = statBuffSheet[buffLinkSheet[RowData.Id].LinkedBuffId];
            var frostBite = BuffFactory.GetStatBuff(row);
            return frostBite;
        }
    }
}
