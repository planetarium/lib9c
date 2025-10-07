using System;
using System.Collections.Generic;
using System.Linq;
using Lib9c.Model.Buff;
using Lib9c.Model.Character;
using Lib9c.Model.Elemental;
using Lib9c.Model.Stat;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Skill.Arena
{
    [Serializable]
    public abstract class ArenaSkill : ISkill
    {
        public SkillSheet.Row SkillRow { get; }
        public long Power { get; private set; }
        public int Chance { get; private set; }
        public int StatPowerRatio { get; private set; }
        public StatType ReferencedStatType { get; private set; }

        // When used as model
        [field: NonSerialized]
        public SkillCustomField? CustomField { get; set; }

        protected ArenaSkill(
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

        public abstract BattleStatus.Arena.ArenaSkill Use(
            ArenaCharacter caster,
            ArenaCharacter target,
            int turn,
            IEnumerable<Buff.Buff> buffs
        );

        protected bool Equals(Skill other)
        {
            return SkillRow.Equals(other.SkillRow) && Power == other.Power && Chance.Equals(other.Chance);
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

        protected IEnumerable<BattleStatus.Arena.ArenaSkill.ArenaSkillInfo> ProcessBuff(
            ArenaCharacter caster,
            ArenaCharacter target,
            int turn,
            IEnumerable<Buff.Buff> buffs
        )
        {
            var infos = new List<BattleStatus.Arena.ArenaSkill.ArenaSkillInfo>();
            foreach (var buff in buffs)
            {
                IEnumerable<Buff.Buff> dispelList = null;
                switch (buff.BuffInfo.SkillTargetType)
                {
                    case SkillTargetType.Enemy:
                    case SkillTargetType.Enemies:
                        var affected = true;
                        var dispel = target.Buffs.Values.FirstOrDefault(bf => bf is Dispel);
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

                        infos.Add(GetSkillInfo(target, turn, buff, affected: affected,
                            dispelList: dispelList));
                        break;

                    case SkillTargetType.Self:
                    case SkillTargetType.Ally:
                        dispelList = caster.AddBuff(buff);
                        infos.Add(GetSkillInfo(caster, turn, buff, dispelList: dispelList));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return infos;
        }

        private BattleStatus.Arena.ArenaSkill.ArenaSkillInfo GetSkillInfo(ICloneable target,
            int turn, Buff.Buff buff, bool affected = true,
            IEnumerable<Buff.Buff> dispelList = null)
        {
            return new BattleStatus.Arena.ArenaSkill.ArenaSkillInfo(
                (ArenaCharacter)target.Clone(),
                0,
                false,
                SkillRow.SkillCategory,
                turn,
                ElementalType.Normal,
                SkillRow.SkillTargetType,
                buff,
                affected,
                dispelList
            );
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
    }
}
