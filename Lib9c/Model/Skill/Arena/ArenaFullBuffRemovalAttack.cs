using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill.Arena
{
    /// <summary>
    /// Arena variant of <see cref="FullBuffRemovalAttack"/>.
    /// Deals damage and removes all positive stat buffs from the target.
    /// </summary>
    [Serializable]
    public class ArenaFullBuffRemovalAttack : ArenaAttackSkill
    {
        /// <inheritdoc cref="ArenaAttackSkill(SkillSheet.Row, long, int, int, StatType)"/>
        public ArenaFullBuffRemovalAttack(
            SkillSheet.Row skillRow,
            long power,
            int chance,
            int statPowerRatio,
            StatType referencedStatType) : base(skillRow, power, chance, statPowerRatio, referencedStatType)
        {
        }

        /// <inheritdoc/>
        public override BattleStatus.Arena.ArenaSkill Use(
            ArenaCharacter caster,
            ArenaCharacter target,
            int turn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (ArenaCharacter)caster.Clone();
            var damage = ProcessDamage(caster, target, turn);
            var buff = ProcessBuff(caster, target, turn, buffs);
            target.RemoveAllStatBuffs();

            return new BattleStatus.Arena.ArenaFullBuffRemovalAttack(SkillRow.Id, clone, damage, buff);
        }
    }
}
