using System;
using System.Collections.Generic;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class Vampiric : ActionBuff
    {
        public int Percentage { get; }

        public Vampiric(ActionBuffSheet.Row row, int percentage) : base(row)
        {
            Percentage = percentage;
        }

        public Vampiric(SkillCustomField customField, ActionBuffSheet.Row row) : base(customField, row)
        {
            Percentage = customField.BuffValue;
        }

        protected Vampiric(Vampiric value) : base(value)
        {
            Percentage = value.Percentage;
        }

        public override object Clone()
        {
            return new Vampiric(this);
        }

        public BattleStatus.Skill GiveEffect(CharacterBase affectedCharacter, BattleStatus.Skill.SkillInfo skillInfo, int simulatorWaveTurn, bool copyCharacter = true)
        {
            var target = copyCharacter ? (CharacterBase) affectedCharacter.Clone() : null;
            var effect = (int)(skillInfo.Effect * Percentage / 100m);
            affectedCharacter.Heal(effect);
            // Copy new Character with healed.
            var infos = new List<BattleStatus.Skill.SkillInfo>
            {
                new(affectedCharacter.Id,
                    affectedCharacter.IsDead,
                    affectedCharacter.Thorn,
                    effect,
                    false,
                    SkillCategory.Heal,
                    simulatorWaveTurn,
                    RowData.ElementalType,
                    RowData.TargetType,
                    target: target)
            };
            return new BattleStatus.Tick(RowData.Id,
                target,
                infos,
                ArraySegment<BattleStatus.Skill.SkillInfo>.Empty);
        }

        public ArenaSkill GiveEffectForArena(ArenaCharacter affectedCharacter, ArenaSkill.ArenaSkillInfo skillInfo, int simulatorWaveTurn)
        {
            var clone = (ArenaCharacter)affectedCharacter.Clone();
            var effect = (int)(skillInfo.Effect * Percentage / 100m);
            affectedCharacter.Heal(effect);
            // Copy new Character with healed.
            var infos = new List<ArenaSkill.ArenaSkillInfo>
            {
                new(affectedCharacter,
                    effect,
                    false,
                    SkillCategory.Heal,
                    simulatorWaveTurn,
                    RowData.ElementalType,
                    RowData.TargetType)
            };
            return new ArenaTick(
                clone,
                infos,
                null);
        }
    }
}
