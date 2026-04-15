using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill
{
    /// <summary>
    /// An attack skill that deals damage and removes all positive stat buffs from the target.
    /// Unlike <see cref="BuffRemovalAttack"/> which removes only the most recent buff,
    /// this skill removes every positive stat buff at once.
    /// </summary>
    [Serializable]
    public class FullBuffRemovalAttack : AttackSkill
    {
        public FullBuffRemovalAttack(
            SkillSheet.Row skillRow,
            long power,
            int chance,
            int statPowerRatio,
            StatType referencedStatType) : base(skillRow, power, chance, statPowerRatio, referencedStatType)
        {
        }

        public override BattleStatus.Skill Use(CharacterBase caster,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs, bool copyCharacter)
        {
            var clone = copyCharacter ? (CharacterBase) caster.Clone() : null;
            var damage = ProcessDamage(caster, simulatorWaveTurn, copyCharacter: copyCharacter);
            var buff = ProcessBuff(caster, simulatorWaveTurn, buffs, copyCharacter);
            var targets = SkillRow.SkillTargetType.GetTarget(caster);
            foreach (var target in targets)
            {
                target.RemoveAllStatBuffs();
            }

            return new Model.BattleStatus.FullBuffRemovalAttack(SkillRow.Id, clone, damage, buff);
        }
    }
}
