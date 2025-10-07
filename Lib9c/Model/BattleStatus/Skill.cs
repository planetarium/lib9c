#nullable enable
using System;
using System.Collections.Generic;
using Lib9c.Model.Buff;
using Lib9c.Model.Character;
using Lib9c.Model.Elemental;
using Lib9c.Model.Skill;

namespace Lib9c.Model.BattleStatus
{
    [Serializable]
    public abstract class Skill : EventBase
    {
        [Serializable]
        public class SkillInfo
        {
            public readonly CharacterBase? Target;
            public readonly long Effect;
            public readonly bool Critical;
            public readonly SkillCategory SkillCategory;
            public readonly ElementalType ElementalType;
            public readonly SkillTargetType SkillTargetType;
            public readonly int WaveTurn;
            public readonly long Thorn;
            public readonly bool IsDead;
            public readonly Guid CharacterId;
            public readonly IEnumerable<Model.Buff.Buff>? DispelList;
            public readonly bool Affected;

            public readonly Model.Buff.Buff? Buff;
            public readonly IceShield? IceShield;

            public SkillInfo(Guid characterId, bool isDead, long thorn, long effect, bool critical,
                SkillCategory skillCategory,
                int waveTurn, ElementalType elementalType = ElementalType.Normal,
                SkillTargetType targetType = SkillTargetType.Enemy, Model.Buff.Buff? buff = null,
                CharacterBase? target = null,
                bool affected = true,
                IEnumerable<Model.Buff.Buff>? dispelList = null,
                IceShield? iceShield = null)
            {
                CharacterId = characterId;
                IsDead = isDead;
                Thorn = thorn;
                Effect = effect;
                Critical = critical;
                SkillCategory = skillCategory;
                ElementalType = elementalType;
                SkillTargetType = targetType;
                Buff = buff;
                WaveTurn = waveTurn;
                Target = target;
                Affected = affected;
                DispelList = dispelList;
                IceShield = iceShield;
            }
        }

        public readonly int SkillId;

        public readonly IEnumerable<SkillInfo> SkillInfos;


        public readonly IEnumerable<SkillInfo>? BuffInfos;

        protected Skill(int skillId, CharacterBase character, IEnumerable<SkillInfo> skillInfos,
            IEnumerable<SkillInfo> buffInfos) : base(character)
        {
            SkillId = skillId;
            SkillInfos = skillInfos;
            BuffInfos = buffInfos;
        }
    }
}
