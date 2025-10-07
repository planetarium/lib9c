#nullable enable
using System;
using System.Collections.Generic;
using Lib9c.Model.Buff;
using Lib9c.Model.Character;
using Lib9c.Model.Elemental;
using Lib9c.Model.Skill;

namespace Lib9c.Model.BattleStatus.Arena
{
    [Serializable]
    public abstract class ArenaSkill : ArenaEventBase
    {
        [Serializable]
        public class ArenaSkillInfo
        {
            public readonly ArenaCharacter Target;
            public readonly long Effect;
            public readonly bool Critical;
            public readonly SkillCategory SkillCategory;
            public readonly ElementalType ElementalType;
            public readonly SkillTargetType SkillTargetType;
            public readonly int Turn;
            public readonly bool Affected;
            public readonly IEnumerable<Model.Buff.Buff>? DispelList;

            public readonly Model.Buff.Buff? Buff;
            public readonly IceShield? IceShield;

            public ArenaSkillInfo(ArenaCharacter character, long effect, bool critical, SkillCategory skillCategory,
                int turn, ElementalType elementalType = ElementalType.Normal,
                SkillTargetType targetType = SkillTargetType.Enemy, Model.Buff.Buff? buff = null,
                bool affected = true,
                IEnumerable<Model.Buff.Buff>? dispelList = null,
                IceShield? iceShield = null)
            {
                Target = character;
                Effect = effect;
                Critical = critical;
                SkillCategory = skillCategory;
                ElementalType = elementalType;
                SkillTargetType = targetType;
                Buff = buff;
                Turn = turn;
                Affected = affected;
                DispelList = dispelList;
                IceShield = iceShield;
            }
        }

        public readonly int SkillId;

        public readonly IEnumerable<ArenaSkillInfo> SkillInfos;

        public readonly IEnumerable<ArenaSkillInfo>? BuffInfos;

        protected ArenaSkill(
            int skillId,
            ArenaCharacter character,
            IEnumerable<ArenaSkillInfo> skillInfos,
            IEnumerable<ArenaSkillInfo> buffInfos)
            : base(character)
        {
            SkillId = skillId;
            SkillInfos = skillInfos;
            BuffInfos = buffInfos;
        }
    }
}
