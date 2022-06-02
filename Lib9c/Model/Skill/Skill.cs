using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Character;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill.Arena;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill
{
    [Serializable]
    public class Skill : IState
    {
        public readonly SkillSheet.Row SkillRow;
        public int Power { get; private set; }
        public int Chance { get; private set; }

        protected Skill(SkillSheet.Row skillRow, int power, int chance)
        {
            SkillRow = skillRow;
            Power = power;
            Chance = chance;
        }

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
                hashCode = (hashCode * 397) ^ Power;
                hashCode = (hashCode * 397) ^ Chance.GetHashCode();
                return hashCode;
            }
        }

        protected IEnumerable<Model.BattleStatus.Skill.SkillInfo> ProcessBuff(
            StageCharacter caster,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs
        )
        {
            var infos = new List<Model.BattleStatus.Skill.SkillInfo>();
            foreach (var buff in buffs)
            {
                var targets = buff.GetTarget(caster);
                foreach (var target in targets.Where(target => target.GetChance(buff.RowData.Chance)))
                {
                    target.AddBuff(buff);
                    infos.Add(new Model.BattleStatus.Skill.SkillInfo((StageCharacter) target.Clone(), 0, false,
                        SkillRow.SkillCategory, simulatorWaveTurn, ElementalType.Normal, SkillRow.SkillTargetType,
                        buff));
                }
            }

            return infos;
        }

        protected IEnumerable<Model.BattleStatus.Skill.SkillInfo> ProcessBuffForArena(
            ArenaPlayer target,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs
        )
        {
            var infos = new List<Model.BattleStatus.Skill.SkillInfo>();
            foreach (var buff in buffs)
            {
                target.AddBuff(buff);
                infos.Add(new Model.BattleStatus.Skill.SkillInfo(target, 0, false,
                    SkillRow.SkillCategory, simulatorWaveTurn, ElementalType.Normal, SkillRow.SkillTargetType,
                    buff));
            }

            return infos;
        }

        public void Update(int chance, int power)
        {
            Chance = chance;
            Power = power;
        }

        public IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "skillRow"] = SkillRow.Serialize(),
                [(Text) "power"] = Power.Serialize(),
                [(Text) "chance"] = Chance.Serialize()
            });
    }
}
