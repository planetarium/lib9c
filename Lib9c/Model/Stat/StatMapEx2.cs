using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Stat
{
    public class StatMapEx2: StatMap2, IStatMapEx
    {
        public StatMapEx2(StatType statType, decimal value = 0m, decimal additionalValue = 0m) : base(statType, value)
        {
            AdditionalValue = additionalValue;
        }

        public StatMapEx2(StatMap statMap) : this(statMap.StatType, statMap.Value)
        {
        }

        public StatMapEx2(Dictionary serialized) : base(serialized)
        {
            AdditionalValue = serialized["additionalValue"].ToDecimal();
        }

        private decimal _additionalValue;

        public bool HasAdditionalValue => AdditionalValue > 0m;

        public decimal AdditionalValue
        {
            get => _additionalValue;
            set
            {
                _additionalValue = value;
                AdditionalValueAsInt = (int)(_additionalValue * 100);
            }

        }
        public int AdditionalValueAsInt { get; set; }
        public decimal TotalValue => Value + AdditionalValueAsInt;
        public int TotalValueAsInt => ValueAsInt + AdditionalValueAsInt;
    }
}
