using System;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class IntStat : ICloneable
    {
        protected bool Equals(IntStat other)
        {
            return Type == other.Type && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((IntStat) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Type * 397) ^ Value;
            }
        }

        public readonly StatType Type;
        public int Value { get; private set; }

        public IntStat()
        {
        }

        protected IntStat(IntStat value)
        {
            Type = value.Type;
            Value = value.Value;
        }

        public IntStat(StatType type, int value = 0)
        {
            Type = type;
            Value = value;
        }

        public virtual void Reset()
        {
            Value = 0;
        }

        public void SetValue(int value)
        {
            Value = value;
        }

        public void AddValue(int value)
        {
            SetValue(Value + value);
        }

        public void AddValue(float value)
        {
            AddValue((int)value);
        }

        public virtual object Clone()
        {
            return new IntStat(this);
        }

        public virtual IValue Serialize()
        {
            return Dictionary.Empty
                .Add("type", StateExtensions.Serialize(Type))
                .Add("value", Value.Serialize());
        }

    }
}
