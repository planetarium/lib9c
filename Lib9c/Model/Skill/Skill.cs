using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill
{
    [Serializable]
    public abstract class Skill : IState, ISkill
    {
        public SkillSheet.Row SkillRow { get; }
        public long Power { get; private set; }
        public int Chance { get; private set; }
        public int StatPowerRatio { get; private set; }
        public StatType ReferencedStatType { get; private set; }

        // When used as model
        [field: NonSerialized]
        public SkillCustomField? CustomField { get; set; }

        protected Skill(
            SkillSheet.Row skillRow,
            long power,
            int chance,
            int statPowerRatio,
            StatType referencedStatType)
        {
            SkillRow = skillRow;
            Power = power;
            Chance = chance;
            StatPowerRatio = statPowerRatio;
            ReferencedStatType = referencedStatType;
        }

        public abstract BattleStatus.Skill Use(CharacterBase caster,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs, bool copyCharacter);

        protected bool Equals(Skill other)
        {
            return SkillRow.Equals(other.SkillRow) &&
                Power == other.Power &&
                Chance.Equals(other.Chance);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Skill) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = SkillRow.GetHashCode();
                hashCode = (hashCode * 397) ^ Power.GetHashCode();
                hashCode = (hashCode * 397) ^ Chance.GetHashCode();
                hashCode = (hashCode * 397) ^ StatPowerRatio.GetHashCode();
                hashCode = (hashCode * 397) ^ ReferencedStatType.GetHashCode();
                return hashCode;
            }
        }

        protected IEnumerable<BattleStatus.Skill.SkillInfo> ProcessBuff(CharacterBase caster,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs, bool copyCharacter)
        {
            var infos = new List<Model.BattleStatus.Skill.SkillInfo>();
            foreach (var buff in buffs)
            {
                var targets = buff.GetTarget(caster);
                foreach (var target in targets.Where(target =>
                             target.GetChance(buff.BuffInfo.Chance)))
                {
                    var affected = true;
                    IEnumerable<Buff.Buff> dispelList = null;
                    var dispel = target.Buffs.Values.FirstOrDefault(bf => bf is Dispel);
                    // Defence debuff if target has dispel
                    if (dispel is not null && buff.IsDebuff())
                    {
                        if (target.Simulator.Random.Next(0, 100) < dispel.BuffInfo.Chance)
                        {
                            affected = false;
                        }
                    }

                    if (affected)
                    {
                        dispelList = target.AddBuff(buff);
                    }

                    infos.Add(new Model.BattleStatus.Skill.SkillInfo(target.Id, target.IsDead,
                        target.Thorn, 0, false,
                        SkillRow.SkillCategory, simulatorWaveTurn, ElementalType.Normal,
                        SkillRow.SkillTargetType,
                        buff, copyCharacter ? (CharacterBase)target.Clone() : target,
                        affected: affected, dispelList: dispelList));
                }
            }

            return infos;
        }

        public void Update(int chance, long power, int statPowerRatio)
        {
            Chance = chance;
            Power = power;
            StatPowerRatio = statPowerRatio;
        }

        public bool IsBuff()
        {
            return SkillRow.SkillType is SkillType.Buff;
        }

        public bool IsDebuff()
        {
            return SkillRow.SkillType is SkillType.Debuff;
        }

        public IValue Serialize()
        {
            var list = new List<IValue>
            {
                SkillRow.Serialize(),
                Power.Serialize(),
                Chance.Serialize()
            };

            if (StatPowerRatio != 0 && ReferencedStatType != StatType.NONE)
            {
                list.Add(StatPowerRatio.Serialize());
                list.Add(ReferencedStatType.Serialize());
            }

            return new List(list);
        }
    }
}
