using Bencodex.Types;

namespace Nekoyume.Model.Stat
{
    public interface IStatMapEx: IStatMap
    {
        public bool HasAdditionalValue { get; }
        public decimal AdditionalValue { get; set; }
        public int AdditionalValueAsInt { get; set; }
        public decimal TotalValue { get; }
        public int TotalValueAsInt { get; }

        public IValue Serialize();
    }
}
