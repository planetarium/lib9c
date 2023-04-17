using System;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class IntStat2 : IIntStat
    {
        public StatType Type { get; }
        public int Value { get; set; }

        public IntStat2()
        {
        }

        protected IntStat2(IntStat2 value)
        {
            Type = value.Type;
            Value = value.Value;
        }

        public IntStat2(StatType type, int value = 0)
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
            Value += value;
        }

        public virtual object Clone()
        {
            return new IntStat2(this);
        }
    }
}
