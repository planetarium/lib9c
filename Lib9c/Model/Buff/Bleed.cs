using System;
using System.Collections.Generic;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;
using Nekoyume.Helper;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class Bleed : ActionBuff
    {
        public long Power { get; }

        public Bleed(ActionBuffSheet.Row row, long power) : base(row)
        {
            Power = power;
        }

        public Bleed(SkillCustomField customField, ActionBuffSheet.Row row) : base(customField, row)
        {
            Power = customField.BuffValue;
        }

        protected Bleed(Bleed value) : base(value)
        {
            Power = value.Power;
        }

        public override object Clone()
        {
            return new Bleed(this);
        }

        public BattleStatus.Skill GiveEffect(CharacterBase affectedCharacter,
            int simulatorWaveTurn, bool copyCharacter = true)
        {
            var clone = copyCharacter ? (CharacterBase) affectedCharacter.Clone() : null;
            var damage = affectedCharacter.GetDamage(Power, false);
            affectedCharacter.CurrentHP -= damage;
            // Copy new Character with damaged.
            var target = copyCharacter ? (CharacterBase) affectedCharacter.Clone() : null;
            var damageInfos = new List<BattleStatus.Skill.SkillInfo>
            {
                new BattleStatus.Skill.SkillInfo(affectedCharacter.Id, affectedCharacter.IsDead, affectedCharacter.Thorn, damage, false,
                    SkillCategory.Debuff, simulatorWaveTurn, RowData.ElementalType,
                    RowData.TargetType, target: target)
            };

            return new Model.BattleStatus.TickDamage(
                RowData.Id,
                clone,
                damageInfos,
                null);
        }

        public ArenaSkill GiveEffectForArena(
            ArenaCharacter affectedCharacter,
            int simulatorWaveTurn)
        {
            var clone = (ArenaCharacter)affectedCharacter.Clone();
            var originalDamage = NumberConversionHelper.SafeDecimalToInt32(decimal.Round(Power * RowData.ATKPowerRatio));
            var damage = affectedCharacter.GetDamage(originalDamage, false);
            affectedCharacter.CurrentHP -= damage;

            var damageInfos = new List<ArenaSkill.ArenaSkillInfo>
            {
                new ArenaSkill.ArenaSkillInfo((ArenaCharacter)affectedCharacter.Clone(), damage, false,
                    SkillCategory.Debuff, simulatorWaveTurn, RowData.ElementalType,
                    RowData.TargetType)
            };

            return new ArenaTickDamage(
                RowData.Id,
                clone,
                damageInfos,
                null);
        }
    }
}
