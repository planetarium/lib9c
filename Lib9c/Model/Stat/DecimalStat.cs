using System;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class DecimalStat : IDecimalStat
    {
        private decimal _value;

        public readonly StatType Type;
        public int Value
        {
            get => (int)(_value / 100);
            set => _value = value;
        }

        public DecimalStat(StatType type, decimal value = 0m)
        {
            Type = type;
            Value = (int) (value * 100);
        }

        public virtual void Reset()
        {
            Value = 0;
        }

        protected DecimalStat(DecimalStat value)
        {
            Type = value.Type;
            Value = value.Value;
        }

        public void SetValue(decimal value)
        {
            Value = (int) (value * 100);
        }

        public void AddValue(decimal value)
        {
            SetValue(Value + value);
        }

        public virtual object Clone()
        {
            return new DecimalStat(this);
        }

        protected bool Equals(DecimalStat other)
        {
            return _value == other._value && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DecimalStat) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_value.GetHashCode() * 397) ^ (int) Type;
            }
        }
    }
}
