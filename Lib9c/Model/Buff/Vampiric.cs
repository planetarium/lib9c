using System;
using System.Collections.Generic;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;
using Nekoyume.Helper;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class Vampiric : ActionBuff
    {
        public long BasisPoint { get; }

        public Vampiric(ActionBuffSheet.Row row, long basisPoint) : base(row)
        {
            BasisPoint = basisPoint;
        }

        public Vampiric(SkillCustomField customField, ActionBuffSheet.Row row) : base(customField, row)
        {
            BasisPoint = customField.BuffValue;
        }

        protected Vampiric(Vampiric value) : base(value)
        {
            BasisPoint = value.BasisPoint;
        }

        public override object Clone()
        {
            return new Vampiric(this);
        }

        /// <summary>
        /// 공격 피해(<paramref name="skillInfo"/> 의 Effect)에 <see cref="BasisPoint"/>(만분율)를 곱한 만큼 회복한다.
        /// 회복량은 long 이라 int.MaxValue 를 넘어도 잘리지 않는다.
        /// </summary>
        /// <param name="affectedCharacter">회복하는 캐릭터.</param>
        /// <param name="skillInfo">흡혈을 일으킨 공격의 결과.</param>
        /// <param name="simulatorWaveTurn">현재 웨이브 턴.</param>
        /// <param name="copyCharacter">전투 로그용으로 캐릭터를 복제할지.</param>
        /// <returns>회복을 기록한 틱.</returns>
        public BattleStatus.Skill GiveEffect(CharacterBase affectedCharacter, BattleStatus.Skill.SkillInfo skillInfo, int simulatorWaveTurn, bool copyCharacter = true)
        {
            var target = copyCharacter ? (CharacterBase) affectedCharacter.Clone() : null;
            var effect = NumberConversionHelper.SafeDecimalToInt64(skillInfo.Effect * (BasisPoint / 10000m));
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
                    target: copyCharacter ? (CharacterBase)affectedCharacter.Clone() : null)
            };
            return new BattleStatus.Tick(RowData.Id,
                target,
                infos,
                ArraySegment<BattleStatus.Skill.SkillInfo>.Empty);
        }

        /// <summary>
        /// 아레나용 <see cref="GiveEffect"/>. 공격 피해에 <see cref="BasisPoint"/>(만분율)를 곱한 만큼 회복한다.
        /// </summary>
        /// <param name="affectedCharacter">회복하는 캐릭터.</param>
        /// <param name="skillInfo">흡혈을 일으킨 공격의 결과.</param>
        /// <param name="simulatorWaveTurn">현재 웨이브 턴.</param>
        /// <returns>회복을 기록한 틱.</returns>
        public ArenaSkill GiveEffectForArena(ArenaCharacter affectedCharacter, ArenaSkill.ArenaSkillInfo skillInfo, int simulatorWaveTurn)
        {
            var clone = (ArenaCharacter)affectedCharacter.Clone();
            var effect = NumberConversionHelper.SafeDecimalToInt64(skillInfo.Effect * (BasisPoint / 10000m));
            affectedCharacter.Heal(effect);
            // Copy new Character with healed.
            var infos = new List<ArenaSkill.ArenaSkillInfo>
            {
                new((ArenaCharacter)affectedCharacter.Clone(),
                    effect,
                    false,
                    SkillCategory.Heal,
                    simulatorWaveTurn,
                    RowData.ElementalType,
                    RowData.TargetType)
            };
            return new ArenaTick(
                RowData.Id,
                clone,
                infos,
                null);
        }
    }
}
