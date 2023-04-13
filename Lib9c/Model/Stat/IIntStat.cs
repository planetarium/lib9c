using System;

namespace Nekoyume.Model.Stat
{
    public interface IIntStat: ICloneable
    {
        public StatType Type { get; }
        public int Value { get; set; }
        public void SetValue(int value);
        public void Reset();
        void AddValue(int value);
    }
}
