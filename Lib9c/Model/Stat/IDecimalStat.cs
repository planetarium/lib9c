using System;

namespace Nekoyume.Model.Stat
{
    public interface IDecimalStat: ICloneable
    {
        public int Value { get; set; }
        public void Reset();
        public void SetValue(decimal value);
        void AddValue(decimal value);
    }
}
