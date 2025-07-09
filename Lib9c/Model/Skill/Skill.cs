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
    /// <summary>
    /// Abstract base class for all skills in the game.
    /// Provides common functionality for skill execution and serialization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Skills are categorized by their type (Attack, Heal, Buff, Debuff) and category
    /// (NormalAttack, DoubleAttack, BlowAttack, etc.). Each skill has power, chance,
    /// and optional stat-based damage ratio.
    /// </para>
    /// <para>
    /// Serialization format has been migrated from Dictionary to List for better
    /// performance and consistency. The new format stores data in the following order:
    /// [0] SkillRow (serialized SkillSheet.Row)
    /// [1] Power (long)
    /// [2] Chance (int)
    /// [3] StatPowerRatio (int, optional - only included if > 0)
    /// [4] ReferencedStatType (StatType, optional - only included if StatPowerRatio > 0)
    /// </para>
    /// <para>
    /// Backward compatibility is maintained through SkillFactory.Deserialize() which
    /// automatically detects and handles both Dictionary and List formats.
    /// </para>
    /// </remarks>
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

        /// <summary>
        /// Determines if this skill is a buff skill.
        /// </summary>
        /// <returns>True if the skill type is Buff, false otherwise.</returns>
        public bool IsBuff()
        {
            return SkillRow.SkillType is SkillType.Buff;
        }

        /// <summary>
        /// Determines if this skill is a debuff skill.
        /// </summary>
        /// <returns>True if the skill type is Debuff, false otherwise.</returns>
        public bool IsDebuff()
        {
            return SkillRow.SkillType is SkillType.Debuff;
        }

        /// <summary>
        /// Serializes the skill to a List format for storage and transmission.
        /// </summary>
        /// <returns>
        /// A List containing the serialized skill data in the following order:
        /// [0] SkillRow (serialized SkillSheet.Row)
        /// [1] Power (long)
        /// [2] Chance (int)
        /// [3] StatPowerRatio (int, optional - only included if > 0)
        /// [4] ReferencedStatType (StatType, optional - only included if StatPowerRatio > 0)
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method implements the new List-based serialization format for better
        /// performance and consistency. The format is more compact than the previous
        /// Dictionary format and maintains all necessary data.
        /// </para>
        /// <para>
        /// Optional fields (StatPowerRatio and ReferencedStatType) are only included
        /// when they have non-default values to minimize serialization overhead.
        /// </para>
        /// <para>
        /// For backward compatibility, use SkillFactory.Deserialize() which can handle
        /// both the new List format and the legacy Dictionary format.
        /// </para>
        /// </remarks>
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
