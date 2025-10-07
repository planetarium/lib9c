using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Model.Skill;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Buff
{
    [Serializable]
    public abstract class ActionBuff : Buff
    {
        private readonly IEnumerable<ActionBuffType> _debuffTypes = new List<ActionBuffType>
        {
            ActionBuffType.Bleed,
            ActionBuffType.Stun,
        };

        public ActionBuffSheet.Row RowData { get; }
        public SkillCustomField? CustomField { get; }

        public ActionBuff(ActionBuffSheet.Row row) : base(
            new BuffInfo(row.Id, row.GroupId, row.Chance, row.Duration, row.TargetType))
        {
            RowData = row;
        }

        public ActionBuff(SkillCustomField customField, ActionBuffSheet.Row row) : base(
            new BuffInfo(row.Id, row.GroupId, row.Chance, customField.BuffDuration, row.TargetType))
        {
            RowData = row;
        }

        protected ActionBuff(ActionBuff value) : base(value)
        {
            RowData = value.RowData;
            CustomField = value.CustomField;
        }

        public override bool IsBuff()
        {
            return !IsDebuff();
        }

        public override bool IsDebuff()
        {
            return _debuffTypes.Contains(RowData.ActionBuffType);
        }
    }
}
