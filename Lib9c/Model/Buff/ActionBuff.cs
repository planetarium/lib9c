using System;
using Lib9c.Model.Character;
using Lib9c.Model.Skill;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Buff
{
    [Serializable]
    public abstract class ActionBuff : Buff
    {
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

        public abstract BattleStatus.Skill GiveEffect(
            CharacterBase affectedCharacter,
            int simulatorWaveTurn);

        public abstract BattleStatus.Arena.ArenaSkill GiveEffectForArena(
            ArenaCharacter affectedCharacter,
            int simulatorWaveTurn);
    }
}
