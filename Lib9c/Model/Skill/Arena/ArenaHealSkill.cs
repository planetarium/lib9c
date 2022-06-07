using System.Collections.Generic;
using Nekoyume.Model.Character;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill.Arena
{
    public class ArenaHealSkill : Skill, IArenaSkill
    {
        public ArenaHealSkill(SkillSheet.Row skillRow, int power, int chance)
            : base(skillRow, power, chance)
        {
        }

        public BattleStatus.Skill Use(
            ArenaCharacter caster,
            ArenaCharacter target,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs)
        {
            var heal = ProcessHeal(caster, simulatorWaveTurn);
            var buff = ProcessBuffForArena(target, simulatorWaveTurn, buffs);

            return new Model.BattleStatus.HealSkill(caster, heal, buff);
        }

        private IEnumerable<BattleStatus.Skill.SkillInfo> ProcessHeal(
            ArenaCharacter caster,
            int simulatorWaveTurn)
        {
            var infos = new List<BattleStatus.Skill.SkillInfo>();
            var healPoint = caster.ATK + Power;
            caster.Heal(healPoint);

            infos.Add(new BattleStatus.Skill.SkillInfo(
                caster,
                healPoint,
                caster.IsCritical(false),
                SkillRow.SkillCategory,
                simulatorWaveTurn));
            return infos;
        }
    }
}
