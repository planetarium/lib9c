using System;
using System.Collections.Generic;
using Lib9c.Helper;
using Lib9c.Model.Character;
using Lib9c.Model.Stat;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Skill.Arena
{
    [Serializable]
    public class ArenaHealSkill : ArenaSkill
    {
        public ArenaHealSkill(
            SkillSheet.Row skillRow,
            long power,
            int chance,
            int statPowerRatio,
            StatType referencedStatType) : base(skillRow, power, chance, statPowerRatio, referencedStatType)
        {
        }

        public override BattleStatus.Arena.ArenaSkill Use(
            ArenaCharacter caster,
            ArenaCharacter target,
            int turn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (ArenaCharacter)caster.Clone();
            var heal = ProcessHeal(caster, turn);
            var buff = ProcessBuff(caster, target, turn, buffs);

            return new BattleStatus.Arena.ArenaHeal(SkillRow.Id, clone, heal, buff);
        }

        private IEnumerable<BattleStatus.Arena.ArenaSkill.ArenaSkillInfo> ProcessHeal(
            ArenaCharacter caster,
            int turn)
        {
            var infos = new List<BattleStatus.Arena.ArenaSkill.ArenaSkillInfo>();

            // Apply stat power ratio
            var powerMultiplier = StatPowerRatio / 10000m;
            var statAdditionalPower = ReferencedStatType != StatType.NONE ?
                NumberConversionHelper.SafeDecimalToInt32(caster.Stats.GetStat(ReferencedStatType) * powerMultiplier) : default;

            var healPoint = caster.ATK + Power + statAdditionalPower;
            caster.Heal(healPoint);

            infos.Add(new BattleStatus.Arena.ArenaSkill.ArenaSkillInfo(
                (ArenaCharacter)caster.Clone(),
                healPoint,
                caster.IsCritical(false),
                SkillRow.SkillCategory,
                turn));

            return infos;
        }
    }
}
