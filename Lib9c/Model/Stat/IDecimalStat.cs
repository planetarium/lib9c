using System;

namespace Nekoyume.Model.Stat
{
    public interface IDecimalStat: ICloneable
    {
        public decimal Value { get; set; }
        public int ValueAsInt { get; }
        public void Reset();
        public void SetValue(decimal value);
        void AddValue(decimal value);
    }
}
