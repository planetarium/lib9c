using System;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class EnhancedDecimalStat : DecimalStat
    {
        public readonly decimal BaseValue;

        public decimal enhancedValue;

        public EnhancedDecimalStat(StatType type, decimal baseValue = 0m, decimal enhancedValue = 0m)
            : base(type, baseValue + enhancedValue)
        {
            BaseValue = baseValue;
            this.enhancedValue = enhancedValue;
        }

        protected EnhancedDecimalStat(EnhancedDecimalStat value) : base(value)
        {
            BaseValue = value.BaseValue;
            enhancedValue = value.enhancedValue;
        }

        protected bool Equals(EnhancedDecimalStat other)
        {
            return base.Equals(other) && BaseValue == other.BaseValue && enhancedValue == other.enhancedValue;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EnhancedDecimalStat) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ BaseValue.GetHashCode();
                hashCode = (hashCode * 397) ^ enhancedValue.GetHashCode();
                return hashCode;
            }
        }
    }
}
