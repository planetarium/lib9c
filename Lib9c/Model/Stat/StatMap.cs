using Bencodex.Types;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Model.Stat
{
    /// <summary>
    /// Represents a collection of character statistics.
    /// Supports both Dictionary and List serialization formats for backward compatibility.
    ///
    /// <para>
    /// Serialization Format:
    /// - Dictionary (Legacy): Uses key-value pairs for backward compatibility
    /// - List (New): Uses ordered list for better performance and smaller size
    /// </para>
    ///
    /// <para>
    /// Field Order (List Format):
    /// 1. version - Serialization version number
    /// 2. stats - List of DecimalStat objects ordered by StatType
    /// </para>
    /// </summary>
    /// <remarks>
    /// This class implements dual serialization support to ensure smooth migration
    /// from the legacy Dictionary format to the new List format. The List format
    /// provides better performance and smaller serialized data size.
    ///
    /// <para>
    /// Example usage:
    /// <code>
    /// // Create stat map
    /// var statMap = new StatMap();
    /// statMap[StatType.HP].SetBaseValue(100);
    /// statMap[StatType.ATK].SetBaseValue(50);
    ///
    /// // Serialize to List format (new)
    /// var serialized = statMap.Serialize(); // Returns List
    ///
    /// // Deserialize from any format
    /// var deserialized = new StatMap(serialized); // Supports both Dictionary and List
    /// </code>
    /// </para>
    /// </remarks>
    [Serializable]
    public class StatMap : IStats, IBaseAndAdditionalStats, IState
    {
        // Serialization version for backward compatibility
        private const int SerializationVersion = 1;

        // Field count constants for serialization
        private const int StatMapFieldCount = 2; // version + stats

        public DecimalStat this[StatType type]
        {
            get => _statMap[type];
        }

        public long HP => _statMap[StatType.HP].TotalValueAsLong;
        public long ATK => _statMap[StatType.ATK].TotalValueAsLong;
        public long DEF => _statMap[StatType.DEF].TotalValueAsLong;
        public long CRI => _statMap[StatType.CRI].TotalValueAsLong;
        public long HIT => _statMap[StatType.HIT].TotalValueAsLong;
        public long SPD => _statMap[StatType.SPD].TotalValueAsLong;
        public long DRV => _statMap[StatType.DRV].TotalValueAsLong;
        public long DRR => _statMap[StatType.DRR].TotalValueAsLong;
        public long CDMG => _statMap[StatType.CDMG].TotalValueAsLong;
        public long ArmorPenetration => _statMap[StatType.ArmorPenetration].TotalValueAsLong;
        public long Thorn => _statMap[StatType.Thorn].TotalValueAsLong;

        public long BaseHP => _statMap[StatType.HP].BaseValueAsLong;
        public long BaseATK => _statMap[StatType.ATK].BaseValueAsLong;
        public long BaseDEF => _statMap[StatType.DEF].BaseValueAsLong;
        public long BaseCRI => _statMap[StatType.CRI].BaseValueAsLong;
        public long BaseHIT => _statMap[StatType.HIT].BaseValueAsLong;
        public long BaseSPD => _statMap[StatType.SPD].BaseValueAsLong;
        public long BaseDRV => _statMap[StatType.DRV].BaseValueAsLong;
        public long BaseDRR => _statMap[StatType.DRR].BaseValueAsLong;
        public long BaseCDMG => _statMap[StatType.CDMG].BaseValueAsLong;
        public long BaseArmorPenetration => _statMap[StatType.ArmorPenetration].BaseValueAsLong;
        public long BaseThorn => _statMap[StatType.Thorn].BaseValueAsLong;

        public long AdditionalHP => _statMap[StatType.HP].AdditionalValueAsLong;
        public long AdditionalATK => _statMap[StatType.ATK].AdditionalValueAsLong;
        public long AdditionalDEF => _statMap[StatType.DEF].AdditionalValueAsLong;
        public long AdditionalCRI => _statMap[StatType.CRI].AdditionalValueAsLong;
        public long AdditionalHIT => _statMap[StatType.HIT].AdditionalValueAsLong;
        public long AdditionalSPD => _statMap[StatType.SPD].AdditionalValueAsLong;
        public long AdditionalDRV => _statMap[StatType.DRV].AdditionalValueAsLong;
        public long AdditionalDRR => _statMap[StatType.DRR].AdditionalValueAsLong;
        public long AdditionalCDMG => _statMap[StatType.CDMG].AdditionalValueAsLong;
        public long AdditionalArmorPenetration => _statMap[StatType.ArmorPenetration].AdditionalValueAsLong;
        public long AdditionalThorn => _statMap[StatType.Thorn].AdditionalValueAsLong;

        private readonly Dictionary<StatType, DecimalStat> _statMap =
            new Dictionary<StatType, DecimalStat>(StatTypeComparer.Instance)
            {
                { StatType.HP, new DecimalStat(StatType.HP) },
                { StatType.ATK, new DecimalStat(StatType.ATK) },
                { StatType.DEF, new DecimalStat(StatType.DEF) },
                { StatType.CRI, new DecimalStat(StatType.CRI) },
                { StatType.HIT, new DecimalStat(StatType.HIT) },
                { StatType.SPD, new DecimalStat(StatType.SPD) },
                { StatType.DRV, new DecimalStat(StatType.DRV) },
                { StatType.DRR, new DecimalStat(StatType.DRR) },
                { StatType.CDMG, new DecimalStat(StatType.CDMG) },
                { StatType.ArmorPenetration, new DecimalStat(StatType.ArmorPenetration) },
                { StatType.Thorn, new DecimalStat(StatType.Thorn) },
            };

        public StatMap()
        {
        }

        public StatMap(StatMap statMap)
        {
            foreach (var stat in statMap.GetDecimalStats(false))
            {
                _statMap[stat.StatType] = (DecimalStat)stat.Clone();
            }
        }

        /// <summary>
        /// Constructor for deserialization that supports both Dictionary and List formats.
        /// </summary>
        /// <param name="serialized">Serialized data in either Dictionary or List format</param>
        /// <exception cref="ArgumentNullException">Thrown when serialized is null</exception>
        /// <exception cref="ArgumentException">Thrown when serialized format is not supported</exception>
        public StatMap(IValue serialized)
        {
            if (serialized == null)
            {
                throw new ArgumentNullException(nameof(serialized), "Serialized data cannot be null");
            }

            switch (serialized)
            {
                case Dictionary dict:
                    DeserializeFromDictionary(dict);
                    break;
                case List list:
                    DeserializeFromList(list);
                    break;
                default:
                    throw new ArgumentException(
                        $"Unsupported serialization format: {serialized.GetType().Name}. " +
                        $"Expected Dictionary or List, got {serialized.GetType().Name}. " +
                        $"This may indicate corrupted data or an unsupported serialization format.");
            }
        }

        /// <summary>
        /// Deserializes data from Dictionary format (legacy support).
        /// </summary>
        /// <param name="dict">Dictionary containing serialized data</param>
        private void DeserializeFromDictionary(Dictionary dict)
        {
#pragma warning disable LAA1002
            foreach (KeyValuePair<IKey, IValue> kv in dict)
            {
                _statMap[StatTypeExtension.Deserialize((Binary)kv.Key)]
                    .Deserialize((Dictionary)kv.Value);
            }
#pragma warning restore LAA1002
        }

        /// <summary>
        /// Deserializes data from List format (new format).
        /// Order: [version, stats]
        /// </summary>
        /// <param name="list">List containing serialized data</param>
        private void DeserializeFromList(List list)
        {
            // Check if we have enough fields for StatMap
            if (list.Count < StatMapFieldCount)
            {
                var fieldNames = string.Join(", ", GetFieldNames());
                throw new ArgumentException(
                    $"Invalid list length for {GetType().Name}: expected at least {StatMapFieldCount}, got {list.Count}. " +
                    $"Required fields: {fieldNames}. " +
                    $"This may indicate corrupted data or an unsupported serialization format.");
            }

            // Always read STAT_MAP_FIELD_COUNT fields
            // version (index 0)
            var version = ((Integer)list[0]).Value;
            if (version != SerializationVersion)
            {
                throw new ArgumentException(
                    $"Unsupported serialization version: {version}. " +
                    $"Expected {SerializationVersion}. " +
                    $"This may indicate corrupted data or an unsupported serialization format.");
            }

            // stats (index 1)
            var statsList = (List)list[1];
            foreach (var statValue in statsList)
            {
                var stat = new DecimalStat((Dictionary)statValue);
                _statMap[stat.StatType] = stat;
            }
        }

        public void Reset()
        {
            foreach (var property in _statMap.Values)
            {
                property.Reset();
            }
        }

        public long GetStatAsLong(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.TotalValueAsLong;
        }

        public decimal GetStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.TotalValue;
        }

        public long GetBaseStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.BaseValueAsLong;
        }

        public long GetAdditionalStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.AdditionalValueAsLong;
        }

        public IEnumerable<(StatType statType, long value)> GetStats(bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap.OrderBy(x => x.Key))
            {
                if (ignoreZero)
                {
                    if (stat.HasTotalValueAsLong)
                    {
                        yield return (statType, stat.TotalValueAsLong);
                    }
                }
                else
                {
                    yield return (statType, stat.TotalValueAsLong);
                }
            }
        }

        public IEnumerable<(StatType statType, long baseValue)> GetBaseStats(bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap.OrderBy(x => x.Key))
            {
                if (ignoreZero)
                {
                    if (stat.HasBaseValueAsLong)
                    {
                        yield return (statType, stat.BaseValueAsLong);
                    }
                }
                else
                {
                    yield return (statType, stat.BaseValueAsLong);
                }
            }
        }

        public IEnumerable<(StatType statType, long additionalValue)> GetAdditionalStats(bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap.OrderBy(x => x.Key))
            {
                if (ignoreZero)
                {
                    if (stat.HasAdditionalValueAsLong)
                    {
                        yield return (statType, stat.AdditionalValueAsLong);
                    }
                }
                else
                {
                    yield return (statType, stat.AdditionalValueAsLong);
                }
            }
        }

        public IEnumerable<(StatType statType, long baseValue, long additionalValue)> GetBaseAndAdditionalStats(
            bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap.OrderBy(x => x.Key))
            {
                if (ignoreZero)
                {
                    if (stat.HasBaseValueAsLong || stat.HasAdditionalValueAsLong)
                    {
                        yield return (statType, stat.BaseValueAsLong, stat.AdditionalValueAsLong);
                    }
                }
                else
                {
                    yield return (statType, stat.BaseValueAsLong, stat.AdditionalValueAsLong);
                }
            }
        }

        public IEnumerable<DecimalStat> GetDecimalStats(bool ignoreZero)
        {
            var values = _statMap.OrderBy(x => x.Key).Select(x => x.Value);
            return ignoreZero ?
                values.Where(x => x.HasBaseValueAsLong || x.HasAdditionalValueAsLong) :
                values;
        }

        /// <summary>
        /// Serializes the StatMap to List format (new format).
        /// Order: [version, stats]
        /// </summary>
        /// <returns>List containing serialized data</returns>
        public IValue Serialize()
        {
            var statsWithValues = _statMap
                .Where(x => x.Value.HasBaseValue || x.Value.HasAdditionalValue)
                .OrderBy(x => x.Key)
                .Select(x => x.Value.Serialize());

            return List.Empty
                .Add(SerializationVersion)
                .Add(new List(statsWithValues));
        }

        /// <summary>
        /// Gets the field names for serialization in order.
        /// </summary>
        /// <returns>Array of field names</returns>
        private static string[] GetFieldNames()
        {
            return new[] { "version", "stats" };
        }
    }
}
