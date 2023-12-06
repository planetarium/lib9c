using System;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
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
