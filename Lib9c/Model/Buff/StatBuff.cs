using System;
using Lib9c.Model.Skill;
using Lib9c.Model.Stat;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Buff
{
    [Serializable]
    public class StatBuff : Buff
    {
        public StatBuffSheet.Row RowData { get; }
        public SkillCustomField? CustomField { get; }

        public int Stack { get; private set; }

        public StatBuff(StatBuffSheet.Row row) : base(
            new BuffInfo(row.Id, row.GroupId, row.Chance, row.Duration, row.TargetType))
        {
            RowData = row;
        }

        public StatBuff(SkillCustomField customField, StatBuffSheet.Row row) : base(
            new BuffInfo(row.Id, row.GroupId, row.Chance, customField.BuffDuration, row.TargetType))
        {
            RowData = row;
            CustomField = customField;
        }

        protected StatBuff(StatBuff value) : base(value)
        {
            RowData = value.RowData;
            CustomField = value.CustomField;
            Stack = value.Stack;
        }

        public StatModifier GetModifier()
        {
            var value = CustomField.HasValue ?
                CustomField.Value.BuffValue :
                RowData.Value;

            return new StatModifier(
                RowData.StatType,
                RowData.OperationType,
                value * (Stack + 1));
        }

        public void SetStack(int stack)
        {
            Stack = Math.Min(stack, RowData.MaxStack);
        }

        public override bool IsBuff()
        {
            return !IsDebuff();
        }

        public override bool IsDebuff()
        {
            return RowData.Value < 0 || CustomField?.BuffValue < 0;
        }

        public override object Clone()
        {
            return new StatBuff(this);
        }
    }
}
