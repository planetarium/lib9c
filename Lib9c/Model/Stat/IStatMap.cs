namespace Nekoyume.Model.Stat
{
    public interface IStatMap
    {
        public StatType StatType { get; }

        public bool HasValue { get; }

        public decimal Value { get; set; }

        public int ValueAsInt { get; set; }

    }
}
