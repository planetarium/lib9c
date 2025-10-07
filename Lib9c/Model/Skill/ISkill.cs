using Lib9c.Model.Stat;
using Lib9c.TableData.Skill;

namespace Lib9c.Model.Skill
{
    public interface ISkill
    {
        public SkillSheet.Row SkillRow { get; }
        /// <summary>
        /// Determines damage of `AttackSkill`.
        /// Determines effect of `BuffSkill`.
        /// </summary>
        public long Power { get; }
        public int Chance { get; }
        public int StatPowerRatio { get; }
        public StatType ReferencedStatType { get; }
        public SkillCustomField? CustomField { get; }
        public void Update(int chance, long power, int statPowerRatio);

        public bool IsBuff();
        public bool IsDebuff();
    }
}
