using System;
using System.Collections.Generic;
using Bencodex.Types;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class Buff : ICloneable
    {
        protected bool Equals(Buff other)
        {
            return originalDuration == other.originalDuration && remainedDuration == other.remainedDuration &&
                   Equals(StatModifier, other.StatModifier) && SkillTargetType == other.SkillTargetType &&
                   Id == other.Id && GroupId == other.GroupId && Chance == other.Chance;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Buff) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = originalDuration;
                hashCode = (hashCode * 397) ^ remainedDuration;
                hashCode = (hashCode * 397) ^ (StatModifier != null ? StatModifier.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) SkillTargetType;
                hashCode = (hashCode * 397) ^ Id;
                hashCode = (hashCode * 397) ^ GroupId;
                hashCode = (hashCode * 397) ^ Chance;
                return hashCode;
            }
        }

        public int originalDuration;
        public int remainedDuration;
        public StatModifier StatModifier { get; }
        public SkillTargetType SkillTargetType { get; }
        public int Id { get; }
        public int GroupId { get; }
        public int Chance { get; }


        protected Buff(BuffSheet.Row row)
        {
            originalDuration = remainedDuration = row.Duration;
            StatModifier = row.StatModifier;
            SkillTargetType = row.TargetType;
            Id = row.Id;
            GroupId = row.GroupId;
            Chance = row.Chance;
        }

        protected Buff(Buff value)
        {
            originalDuration = value.originalDuration;
            remainedDuration = value.remainedDuration;
            StatModifier = value.StatModifier;
            SkillTargetType = value.SkillTargetType;
            Id = value.Id;
            GroupId = value.GroupId;
            Chance = value.Chance;
        }

        protected Buff(Dictionary serialized)
        {
            originalDuration = serialized["original_duration"].ToInteger();
            remainedDuration = serialized["remained_duration"].ToInteger();
            StatModifier = serialized["stat_modifier"].ToStatModifier();
            SkillTargetType = serialized["skill_target_type"].ToEnum<SkillTargetType>();
            Id = serialized["id"].ToInteger();
            GroupId = serialized["group_id"].ToInteger();
            Chance = serialized["chance"].ToInteger();
        }

        protected Buff(IValue serialized) : this((Dictionary) serialized)
        {
        }

        public int Use(CharacterBase characterBase)
        {
            var value = 0;
            switch (StatModifier.StatType)
            {
                case StatType.HP:
                    value = characterBase.HP;
                    break;
                case StatType.ATK:
                    value = characterBase.ATK;
                    break;
                case StatType.DEF:
                    value = characterBase.DEF;
                    break;
                case StatType.CRI:
                    value = characterBase.CRI;
                    break;
                case StatType.HIT:
                    value = characterBase.HIT;
                    break;
                case StatType.SPD:
                    value = characterBase.SPD;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return StatModifier.GetModifiedAll(value);
        }

        public IEnumerable<CharacterBase> GetTarget(CharacterBase caster)
        {
            return SkillTargetType.GetTarget(caster);
        }

        public object Clone()
        {
            switch (StatModifier.StatType)
            {
                case StatType.HP:
                    return new HPBuff(this);
                case StatType.ATK:
                    return new AttackBuff(this);
                case StatType.DEF:
                    return new DefenseBuff(this);
                case StatType.CRI:
                    return new CriticalBuff(this);
                case StatType.HIT:
                    return new HitBuff(this);
                case StatType.SPD:
                    return new SpeedBuff(this);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IValue Serialize()
        {
            return Dictionary.Empty
                .Add("original_duration", originalDuration.Serialize())
                .Add("remained_duration", remainedDuration.Serialize())
                .Add("stat_modifier", StatModifier.Serialize())
                .Add("skill_target_type", SkillTargetType.Serialize())
                .Add("id", Id.Serialize())
                .Add("group_id", GroupId.Serialize())
                .Add("chance", Chance.Serialize());
        }
    }
}
