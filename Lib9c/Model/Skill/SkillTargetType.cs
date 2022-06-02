using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Model.Skill
{
    public enum SkillTargetType
    {
        Enemy,
        Enemies,
        Self,
        Ally,
    }

    public static class SkillTargetTypeExtension
    {
        public static IEnumerable<StageCharacter> GetTarget(this SkillTargetType value, StageCharacter caster)
        {
            var targets = caster.Targets;
            IEnumerable<StageCharacter> target;
            switch (value)
            {
                case SkillTargetType.Enemy:
                    target = new[] {targets.First()};
                    break;
                case SkillTargetType.Enemies:
                    target = caster.Targets;
                    break;
                case SkillTargetType.Self:
                    target = new[] {caster};
                    break;
                case SkillTargetType.Ally:
                    target = new[] {caster};
                    break;
                default:
                    target = new[] {targets.First()};
                    break;
            }

            return target;
        }
    }
}
