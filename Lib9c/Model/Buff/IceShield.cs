using System;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class IceShield : ActionBuff
    {
        public long Power { get; }
        public IceShield(ActionBuffSheet.Row row, long power) : base(row)
        {
            Power = power;
        }

        public IceShield(SkillCustomField customField, ActionBuffSheet.Row row) : base(customField, row)
        {
            Power = customField.BuffValue;
        }

        protected IceShield(IceShield value) : base(value)
        {
            Power = value.Power;
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
